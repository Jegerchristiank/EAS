# ESG Rapportering — TypeScript/React Handleplan

Denne handleplan beskriver, hvordan vi omskriver løsningen til et TypeScript-baseret setup med fokus på en webapp først og senere offline-understøttede mobilapps (Expo). Dokumentet fungerer som "kode uden kode" – et detaljeret blueprint, som vi kan vende tilbage til, når vi skal implementere funktionerne trin for trin.

## 1. Produktmål & Rammer
- **Output:** Webapplikation der kan indsamle ESG-data (B1-11, C1-9), udføre komplekse beregninger og generere en PDF-rapport til download.
- **Platformer på sigt:** Web (først), mobil via Expo/React Native (med offline support) som fase 2.
- **Datahåndtering:** Ingen login eller vedvarende backend-lagring i første version – brugerens indtastninger lever lokalt i browseren (localStorage) og i mobilappens secure storage.
- **Hosting:** Self-hosted (fx Docker-container eller Node-server på egen VPS).
- **AI-workflow:** Få og veldefinerede scripts (pnpm based), klar mappe- og komponentstruktur, dokumenteret i en AI-playbook.

## 2. Arkitektur (Monorepo med pnpm)
```
/ (repo-rod)
├─ package.json            # scripts til turborepo/pnpm workspaces
├─ turbo.json              # orkestrerer build/dev/test på tværs af pakker
├─ pnpm-workspace.yaml
├─ packages
│  ├─ web/                 # Next.js 14 (App Router)
│  │  ├─ app/              # routes (server actions, layout, etc.)
│  │  ├─ components/       # genbrugelige UI-komponenter
│  │  ├─ features/
│  │  │   ├─ wizard/       # formular-wizard & state-management
│  │  │   ├─ calculations/ # hooks der kalder shared-logik
│  │  │   └─ pdf/          # UI til PDF-download & visning
│  │  ├─ lib/
│  │  │   ├─ schema/       # loader/validerer inputskemaer
│  │  │   ├─ storage/      # localStorage-adapter, eksport/import
│  │  │   └─ pdf-client.ts # kalder server actions eller edge-funktion
│  │  ├─ public/           # statiske assets, fonte til PDF, etc.
│  │  ├─ server/           # server actions, PDF-generation, edge handlers
│  │  └─ tests/            # Playwright + vitest for web
│  ├─ mobile/              # Expo (React Native)
│  │  ├─ app/              # Expo Router (fase 2)
│  │  ├─ components/
│  │  └─ storage/          # offline persistence via MMKV/SecureStore
│  ├─ shared/              # TypeScript bibliotek
│  │  ├─ calculations/     # pure functions for B1-11 & C1-9
│  │  ├─ schema/           # typer + parser af CSV/datapunkter
│  │  ├─ pdf/              # PDF layout-definition (React-pdf eller @react-pdf/renderer)
│  │  ├─ utils/            # fælles helpers (rounding, unit conversion)
│  │  └─ tests/            # vitest for shared logik
│  └─ tooling/             # scripts (csv → schema, lint configs)
├─ docs/
│  ├─ ts-react-handleplan.md  # dette dokument
│  └─ ai-playbook.md          # kommandoer, retningslinjer til AI
└─ .github/workflows/      # CI pipelines (pnpm install, lint, test)
```

### Kerneprincipper
- **Shared first:** Alt der kan genbruges (beregninger, schema, pdf-layout) ligger i `packages/shared`, så web og mobil bruger samme logik.
- **Server actions frem for API:** Next.js server actions genererer PDF’en og returnerer en Base64/Blob til klienten → ingen separat API-service nødvendig.
- **Offline-friendly design:** Formularstate ligger i klienten og kan eksporteres/importeres som JSON. Mobilen vil bruge samme schema og beregninger offline.
- **Konfigurerbar PDF-motor:** Start med `@react-pdf/renderer` (server-side). Hvis vi senere skal bruge bedre typografi, kan vi switche til `puppeteer` + HTML-skabelon uden at ændre domain-logikken.

## 3. Data & Skemaer
1. **Eksisterende CSV (docs/ESG_datapunkter__B1-B11__C1-C9_.csv)** bruges til at generere et JSON-skema.
2. `packages/tooling/schema-generator.ts` læser CSV’en og skriver to artefakter til `packages/shared/schema`:
   - `esg-input-schema.json`: struktur for wizard (sektioner, felter, typer, betingelser).
   - `esg-formula-map.json`: mapping fra datapunkt → beregningsfunktion.
3. Ved build tid importerer `packages/shared/schema/index.ts` JSON’en og eksporterer TypeScript-typer (`EsgInput`, `EsgModuleId`, etc.).
4. Web-wizarden renderer automatisk input baseret på schemaet → minimale manuelle formularer.

## 4. Beregningsmotor (Pseudo-kode)
```ts
// packages/shared/calculations/modules.ts
export type ModuleId = "B1" | ... | "C9";

export interface ModuleInput {
  datapoints: Record<string, number | boolean | string | null>;
  factors: FactorSnapshot;
}

export interface ModuleResult {
  id: ModuleId;
  value: number | boolean | string;
  unit?: string;
  assumptions: string[];
  trace: CalculationTrace[];
  warnings: string[];
}

export function runModule(id: ModuleId, input: ModuleInput): ModuleResult {
  switch (id) {
    case "B1":
      return runB1(input);
    // ...
    case "C9":
      return runC9(input);
  }
}

function runB1({ datapoints, factors }: ModuleInput): ModuleResult {
  const scope1Kg = datapoints["B1.scope1_kg"] ?? 0;
  const biogenicShare = datapoints["B1.biogenic_share"] ?? 0;
  const emissionFactor = resolveFactor(factors, "B1.scope1_default");

  const value = scope1Kg * emissionFactor * (1 - biogenicShare);

  return {
    id: "B1",
    value,
    unit: "tCO2e",
    assumptions: [
      "Biogenic share reduces fossil emissions linearly",
      `Factor source: ${emissionFactor.source}`,
    ],
    trace: [
      {
        formula: "scope1Kg * factor * (1 - biogenicShare)",
        inputs: { scope1Kg, biogenicShare, factor: emissionFactor.value },
      },
    ],
    warnings: value < 0 ? ["Negative emission result detected"] : [],
  };
}
```
- Hver `runX`-funktion er **ren og deterministisk**.
- `resolveFactor` håndterer auto/custom-faktorer (læsning fra JSON, fallback til defaults).
- Beregninger valideres via vitest: `expect(runB1(sampleInput).value).toBeCloseTo(…)`.

## 5. PDF-generator (Pseudo-kode)
```tsx
// packages/shared/pdf/report.tsx
import { Document, Page, View, Text } from "@react-pdf/renderer";

export interface PdfProps {
  input: EsgInput;
  results: ModuleResult[];
  generatedAt: string;
  schemaVersion: string;
  factorVersion: string;
}

export function EsgReportPdf({ input, results, generatedAt, schemaVersion, factorVersion }: PdfProps) {
  return (
    <Document>
      <Page size="A4" style={styles.page}>
        <View style={styles.header}>
          <Text>ESG-rapport</Text>
          <Text>Dato: {generatedAt}</Text>
          <Text>Schema v{schemaVersion} • Faktor v{factorVersion}</Text>
        </View>

        {moduleSections(results).map(section => (
          <View key={section.id} style={styles.section}>
            <Text style={styles.sectionTitle}>{section.title}</Text>
            {section.rows.map(row => (
              <View key={row.datapoint} style={styles.row}>
                <Text>{row.label}</Text>
                <Text>{row.valueFormatted}</Text>
                <Text>{row.unit ?? ""}</Text>
              </View>
            ))}
          </View>
        ))}

        <View style={styles.assumptions}>
          <Text style={styles.sectionTitle}>Antagelser</Text>
          {collectAssumptions(results).map(item => (
            <Text key={item}>{"• " + item}</Text>
          ))}
        </View>
      </Page>
    </Document>
  );
}
```
- Server action: `generateReport(input)` → kører `runAllModules`, bygger `EsgReportPdf`, renderer til PDF-buffer (via `renderToBuffer`), returnerer Base64 til klienten.
- Klient: downloader filen (`Blob`) og tilbyder "Gem".

## 6. Webappens user flow
1. **Landing/dashboard**: Kort intro + knap til “Start beregning”.
2. **Wizard (multi-step)**
   - Henter schema via `import schema from "@shared/schema"`.
   - Renderer sektioner dynamisk (radioknapper, checkboxes, talfelter).
   - Gemmer state i `zustand` store + `localStorage` (autosave hver 5. sekund).
   - Viser beregnede resultater i realtid (valgfri) ved at kalde `runModule` i browseren.
3. **Review-side**
   - Opsummering af alle inputs + beregninger.
   - Knap til “Generér PDF” → kalder server action, downloader fil.
4. **Eksport/import**
   - JSON eksport → gemmer nuværende input state.
   - JSON import → loader fra fil.

## 7. Faseinddelt roadmap
### Fase 0 — DevX Setup
- Initialisér monorepo (`pnpm init`, `turbo`, `eslint`, `prettier`, `vitest`).
- Opret `docs/ai-playbook.md` med kommandoer:
  - `pnpm install`
  - `pnpm dev` (starter Next.js)
  - `pnpm lint`
  - `pnpm test`
  - `pnpm schema:generate`
- CI workflow: GitHub Actions med caching og kørsel af lint/test.

### Fase 1 — Shared foundation
- Implementér CSV → schema generator (tooling).
- Definér typer i `packages/shared/schema` + tests.
- Scaffold beregningsmoduler (`runAllModules`, `runB1`-`runC9` stubs) + testfixtures.
- Implementér faktor-læsning (JSON) og helpers (rounding, units).

### Fase 2 — Web-wizard (MVP)
- Next.js app med App Router, Tailwind (eller Mantine/Chakra) for hurtige UI-komponenter.
- Dynamisk wizard komponent, validering (zod baseret på schemaet).
- LocalStorage autosave + JSON eksport/import.
- Simpel resultatside (viser modulresultater live).

### Fase 3 — PDF-integration
- Server action `generateReportAction(input)`.
- Implementér PDF layout + download-knap.
- Tilføj en “test harness” side i dev-mode der viser PDF’en inline (for hurtigere iteration).
- Snapshot-tests for PDF (gemmer reference-buffer og sammenligner hash i CI).

### Fase 4 — Polering & Dokumentation
- Tilføj hjælpetekster, tooltips og inline forklaringer fra schemaet.
- Tilføj `docs/calculation-notes.md` (beregningsantagelser pr. modul).
- Opdater README med setup, brug og deployment.
- Overvej service worker til caching (PWA).

### Fase 5 — Mobil (Expo) [fremtid]
- Expo-projekt i `packages/mobile` med Expo Router.
- Del wizard-komponenter via `react-native-paper` eller `tamagui` + `shared` logik.
- Offline storage via `expo-secure-store` eller `react-native-mmkv`.
- PDF-håndtering: enten generér på device (samme shared PDF logik via `expo-print`) eller ring til self-hosted endpoint når online.

## 8. Command Cheatsheet (foreløbig)
| Formål           | Kommando             |
|------------------|----------------------|
| Installér deps   | `pnpm install`       |
| Start web dev    | `pnpm dev --filter web` |
| Kør lint         | `pnpm lint`          |
| Kør tests        | `pnpm test`          |
| Genér schema     | `pnpm schema:generate` |
| Byg web          | `pnpm build --filter web` |

*Note: Når mobile app introduceres, tilføjer vi `pnpm mobile:start` osv.*

## 9. Åbne spørgsmål / videre planlægning
- Bekræft hvilke beregninger der kræver faktortabeller vs. faste konstanter. 
- Afklar om PDF’en skal have brandede elementer (logo, farver) – ellers holder vi os til et minimalistisk layout.
- Beslut om vi vil understøtte engelsk/dansk skifte fra start eller senere.
- Bestem strategi for offline PDF-generering på mobil (lokal vs. server).

---
Dette dokument bør opdateres løbende, når vi træffer nye valg. Det fungerer som vores fælles reference, så kommende AI-forespørgsler kan pege hertil for kontekst.
