# Manual QA checklist

Denne tjekliste bruges til at verificere den nye ESG-arbejdsflade fra landing-dashboard til administration og imports.

## Navigations- og global funktionalitet
- [ ] Start appen og verificer, at topbar, venstremenu og toast-container vises uden layoutfejl.
- [ ] Skift organisation og periode i topbaren og kontroller, at dashboards opdateres.
- [ ] Test sprogskift (DA/EN) og kontroller, at labels i navigation og dashboard opdateres.
- [ ] Bekræft at fejlpanelet viser diagnostiske beskeder, når der simuleres en API-fejl.
- [ ] Verificer tastaturnavigation: Tab igennem topbar, navigation og hurtigstartkort.

## Landing-dashboard
- [ ] Kontrollér, at kortene for aktive organisationer, perioder, aktiviteter og opgaver viser data.
- [ ] Tryk på en hurtigstart-CTA og verificer, at korrekt side åbnes.
- [ ] Kontrollér, at progress-barer annoncerer procent via aria-attributter.

## Dataindsamling & imports
- [ ] Navigér til /data/energy og bekræft tabelopbygning, statuschips og handlinger.
- [ ] Afprøv drag-and-drop på importsiden og verificer fokusmarkering og ARIA-etiketter.

## Rapporter & analyser
- [ ] På rapportcentralen, test download-/regenerer-knapper og bekæft tilstedeværelsen af checksum-fingerprints.
- [ ] Kontroller metrics-dashboardets kort og trendvisninger.

## Administration
- [ ] Gennemgå listerne for organisationer, perioder og brugere; verificer kolonnejustering og badges.

## Wizard & arbejdslister
- [ ] Start wizard-flowet og kontroller, at trinindikator og valideringshjælpere fungerer som forventet.
- [ ] Gennemgå task-listerne for approvals/afvigelser (placeholder) og verificer filtreringsmuligheder.

## Tilgængelighed og responsivitet
- [ ] Kør browserens Lighthouse audit og sikr minimum 90 i Accessibility.
- [ ] Test layoutet i smal visning (<768px) og bekræft responsiv menu/topbar.
- [ ] Test aria-live på toasts ved at trigge en prøvebesked (brug en midlertidig knap eller `ToastService` via devtools).

## Fejlscenarier
- [ ] Simuler offline API ved at blokere netværk og verificer fallback-data og log-beskeder.
- [ ] Verificer, at ErrorBoundary viser diagnosekode og reload-knap.
