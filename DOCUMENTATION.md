# Documentation — Machine à Café (Cours Test & Industrialisation)

## Contexte

Projet de cours **Test & Industrialisation** (Master 1).  
Objectif : pratiquer le **TDD (Test-Driven Development)** sur un système embarqué simulé — une machine à café — en utilisant des **doubles de test** (Stub, Spy, Fake, Dummy) et en respectant les conventions de lisibilité (Builder, Matcher, AAA).

---

## Architecture générale

Le projet est découpé en 3 couches séparées (3 projets .NET) :

```
Hardware/           → Interfaces matérielles (contrats vers le physique)
MachineACafé/       → Logique software de la machine
MachineACafé.Test/  → Tests et doubles de test
```

---

## Hardware — Interfaces matérielles

> Ce projet définit les **contrats** entre le logiciel et le matériel physique. On ne touche pas à l'implémentation réelle (c'est le rôle du hardware).

| Fichier | Rôle |
|---|---|
| `IBrewer.cs` | Prépare les boissons (café, chocolat, lait…) |
| `IChangeMachine.cs` | Gère les pièces : insertion, rendu, collecte |
| `ICupProvider.cs` | Fournit les gobelets et agitateurs |
| `IButtonPanel.cs` | Panneau de boutons physique |
| `INfcTransceiver.cs` | Lecteur NFC : débite ou recharge une clé café |
| `NfcState.cs` | États NFC : `NoDevice`, `PrepaidDevicePresent`, `RefillableDevicePresent` |
| `CoinCode.cs` | Enum des valeurs de pièces acceptées |
| `ButtonCode.cs` | Enum des codes boutons du panneau |

### INfcTransceiver — détail

```csharp
public interface INfcTransceiver
{
    event Action<NfcState> NfcStateChanged;       // déclenché quand une clé apparaît/disparaît
    bool TryChargeAmount(ushort amountInCents);   // débite un montant, retourne true si OK
    bool TryRefillDevice(ushort amountInCents);   // recharge une clé, retourne true si OK
}
```

`TryChargeAmount` retourne `false` dans tous les cas d'échec (solde insuffisant, erreur, absence de clé) — on ne peut pas distinguer la cause depuis le software.

---

## MachineACafé — Logique software

### `SoftwareMachine.cs`

Classe principale. Reçoit ses dépendances par injection de constructeur.

```csharp
public SoftwareMachine(IBrewer brewer, IChangeMachine changeMachine, INfcTransceiver nfcTransceiver)
```

#### Flux paiement par pièce

```
RegisterMoneyInsertedCallback → Insérer(coin)
  ├─ somme < 40cts → FlushStoredMoney()
  └─ somme >= 40cts → MakeACoffee()
       ├─ succès → CollectStoredMoney()
       └─ échec  → FlushStoredMoney()
```

#### Flux paiement NFC (feature Clé Café — Niveau Simple)

```
NfcStateChanged(état)
  ├─ état != PrepaidDevicePresent → ignoré
  └─ TryChargeAmount(40)
       ├─ false → ignoré (solde insuffisant ou erreur)
       └─ true  → MakeACoffee()
            ├─ succès → café servi
            └─ échec  → TryRefillDevice(40)  [remboursement de la clé]
```

> **Règle métier** : en cas de panne du brewer après un débit NFC, la machine tente de rembourser les 40cts sur la clé via `TryRefillDevice`.

### `Coin.cs`

Modèle représentant une pièce avec sa valeur en centimes.

---

## MachineACafé.Test — Tests

### Conventions

- Framework : **xUnit**
- Langue des tests : **français**
- Structure des tests : **ETANT DONNE / QUAND / ALORS** (Given / When / Then)
- Pas de mocks automatiques — tout est fait à la main

### Doubles de test

#### Brewer

| Classe | Pattern | Comportement |
|---|---|---|
| `BrewerStub` | Stub | Toutes les méthodes retournent `true` |
| `BrewerSpy` | Spy | Wrappeur — compte les appels à `MakeACoffee` |
| `BrewerDummy` | Dummy | Lève une exception sur tout appel (simule une panne) |

#### ChangeMachine

| Classe | Pattern | Comportement |
|---|---|---|
| `ChangeMachineStub` | Stub | Ignore tous les appels |
| `ChangeMachineFake` | Fake | Simule le vrai comportement + `SimulerInsertionPièce()` |
| `ChangeMachineSpy` | Spy | Wrappeur — compte `FlushStoredMoney` et `CollectStoredMoney` |

#### NfcTransceiver

| Classe | Pattern | Comportement |
|---|---|---|
| `NfcTransceiverStub` | Stub | Ignore tout, retourne `false` |
| `NfcTransceiverFake` | Fake | Résultat configurable + `SimulerApparitionCle(NfcState)` |
| `NfcTransceiverSpy` | Spy | Wrappeur — compte les appels et enregistre le montant débité |

### Builder

`SoftwareMachineBuilder` — construit une `SoftwareMachine` pour les tests avec des valeurs par défaut neutres (Stubs).

```csharp
new SoftwareMachineBuilder()
    .AyantUnBrewer(brewer)
    .AyantUneChangeMachine(changeMachine)
    .AyantUnNfcTransceiver(nfcTransceiver)
    .Build();
```

### Matcher

`BrewerAssert` — assertions lisibles sur l'état du brewer :

```csharp
BrewerAssert.CaféPréparé(brewer);  // Assert.Equal(1, brewer.MakeACoffeeInvocations)
BrewerAssert.AucunCafé(brewer);    // Assert.Equal(0, brewer.MakeACoffeeInvocations)
```

### Tests existants

#### `SoftwareMachineTest.cs` — Paiement par pièce

| Test | Scénario |
|---|---|
| `AucuneAction` | La machine ne fait rien au démarrage |
| `CasNominal` | 50cts insérés → café servi + monnaie collectée |
| `CasBrewerDéfaillant` | 50cts insérés + panne brewer → monnaie rendue |
| `PasAssezArgent` | 20cts insérés → monnaie rendue, pas de café |

#### `NfcPaymentTest.cs` — Feature Clé Café NFC (Niveau Simple)

| Test | Scénario |
|---|---|
| `CaféServi_QuandCléNfcPrépayéeEtSoldeSuffisant` | Happy path — café servi |
| `CaféNonServi_QuandCléRechargeable` | Clé rechargeable ignorée |
| `CaféNonServi_QuandSoldeInsuffisant` | Débit refusé → pas de café |
| `TryChargeAmountNonAppelé_QuandAucunDispositif` | NoDevice → rien ne se passe |
| `TryChargeAmountNonAppelé_QuandMachineAuRepos` | Démarrage → aucun appel NFC |
| `CaféNonServi_QuandBrewerDéfaillantEtCléNfc` | Panne brewer → appel tenté, exception absorbée |
| `TryChargeAmountNonAppelé_QuandAucunDispositif` | Événement NoDevice → `TryChargeAmount` non appelé |
| `TryChargeAmountAppeléAvecExactement40Centimes` | Vérifie le montant débité = 40cts |

#### `CoinTest.cs` — Modèle Coin

Tests unitaires sur les valeurs valides et invalides de pièces.

---

## Démarche TDD suivie

Chaque feature est développée en micro-commits avec le cycle :

```
[RED]     → test écrit, ne compile pas ou échoue
[GREEN]   → code minimal pour faire passer le test
[REFACTO] → nettoyage sans casser les tests
```

L'arbre git est découpé pour justifier chaque décision : chaque commit raconte une étape du raisonnement.

---

## Features

| Feature | Statut | Branche |
|---|---|---|
| Paiement par pièce | Terminé | `main` / `dev` |
| Clé Café NFC — Niveau Simple | Terminé | `dev` |
| Clé Café NFC — Niveau Complexe | À venir | — |
