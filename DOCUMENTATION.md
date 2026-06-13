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

// Raccourcis NFC :
new SoftwareMachineBuilder().AyantUneClé(clé).Build();           // clé pré-payée (niveau simple)
new SoftwareMachineBuilder().AyantUnBadgeRecharge(badge).Build(); // badge rechargeable (niveau complexe)
```

### Matcher

`BrewerAssert` — assertions lisibles sur l'état du brewer :

```csharp
BrewerAssert.CaféPréparé(brewer);  // Assert.Equal(1, brewer.MakeACoffeeInvocations)
BrewerAssert.AucunCafé(brewer);    // Assert.Equal(0, brewer.MakeACoffeeInvocations)
```

### Helpers NFC

| Classe | Rôle |
|---|---|
| `CléNfc` | Enveloppe `NfcTransceiverFake` + `NfcTransceiverSpy` — simule présentation/retrait d'une clé pré-payée. Expose `NombreDébits`, `NombreRemboursements`, `DernierMontantDébité`, `DernierMontantRemboursé`. |
| `BadgeRecharge` | Enveloppe `NfcTransceiverFake` + `NfcTransceiverSpy` — simule session recharge d'un badge rechargeable. Expose `NombreRecharges`, `DernierMontantRechargé`. |

### Tests existants

#### `SoftwareMachineTest.cs` — Paiement par pièce

| Test | Scénario |
|---|---|
| `AucuneAction` | La machine ne fait rien au démarrage |
| `CasNominal` | 50cts insérés → café servi + monnaie collectée |
| `CasBrewerDéfaillant` | 50cts insérés + panne brewer → monnaie rendue |
| `PasAssezArgent` | 20cts insérés → monnaie rendue, pas de café |

#### `NfcPaymentTest.cs` — Clé Café NFC (Niveau Simple)

| Test | Scénario |
|---|---|
| `CaféServi_QuandSoldeSuffisant` | Happy path — café servi |
| `CaféNonServi_QuandCléRechargeable` | Clé rechargeable → pas de café |
| `AucunDébit_QuandCléRechargeable` | Clé rechargeable → aucun débit tenté |
| `CaféNonServi_QuandDébitRefusé` | Solde insuffisant → pas de café |
| `AucunRemboursement_QuandSoldeInsuffisant` | Débit refusé → aucun remboursement |
| `AucunDébit_QuandAucunDispositif` | NoDevice → aucun débit |
| `AucunDébit_QuandMachineAuRepos` | Démarrage → aucun débit |
| `PréparationTentée_QuandBrewerEnPanneAprèsDébit` | Panne brewer → tentative quand même |
| `ArgentRemboursé_QuandBrewerEnPanneAprèsDébit` | Panne brewer → 40cts remboursés |
| `AucunRemboursement_QuandCaféPréparéAvecSuccès` | Brewer OK → aucun remboursement |
| `DeuxCafésPréparés_QuandCléPrésentéeDeuxFoisDeSuite` | 2× présentation OK → 2 cafés |
| `AucunCafé_QuandDeuxPrésentationsEtSoldeInsuffisant` | 2× présentation sans solde → 0 café |
| `DeuxRemboursements_QuandDeuxPrésentationsAvecBrewerEnPanne` | 2× présentation + panne → 2 remboursements |
| `DeuxCafésPréparés_QuandRetraitEntreLesDeuxPrésentations` | Retrait entre 2 présentations → 2 cafés |
| `UnCafé_QuandCléRechargeableAvantCléPréPayée` | Rechargeable puis pré-payée → 1 seul café |
| `MontantDébité_EstExactementLePrixDUnCafé` | Montant débité = exactement 0,40 € |

#### `RechargeNfcTest.cs` — Clé Café NFC (Niveau Complexe)

| Test | Cas | Scénario |
|---|---|---|
| `RechargeEffectuée_QuandBadgePrésentEtPièceInsérée` | 1.1 | Badge posé + pièce → clé créditée + pièce encaissée |
| `DeuxRechargesEffectuées_QuandDeuxPiècesInsérées` | 1.2 | 2 pièces successives → 2 recharges + 2 encaissements |
| `CaféServiEtCléDébitéeDe40Cts_QuandBadgePréPayéPrésenté` | 1.3 | Badge pré-payé → 40cts débités + café servi |
| `RechargeAcceptée_QuandPièceAtteinLePlafond` | 2.1 | Pièce amenant au plafond → recharge acceptée |
| `PièceRendue_QuandRechargeRefusée` | 2.2 / 2.3 / 3.1 | Recharge refusée (plafond ou perte NFC) → pièce rendue |
| `AucunCafé_QuandSoldeInsuffisantPourPayer` | 2.4 | Solde < 0,40 € → café refusé |
| `UnSeulCrédit_QuandBadgeRetiréEntreLesDeuxPièces` | 3.2 | Retrait badge entre 2 pièces → 1 seule recharge |
| `AucunEffet_QuandBadgePosépuisRetiréSansPièce` | 3.3 | Badge posé puis retiré sans pièce → aucun effet |
| `AucunCafé_QuandConnexionPerduePendantLeDébit` | 3.4 | Connexion perdue pendant débit café → pas de café |
| `UnSeulCrédit_QuandDoubleSignalPourLaMêmePièce` | 4.1 | Double signal hardware pour 1 pièce → 1 seule recharge |

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
| Clé Café NFC — Niveau Complexe (Recharge avec des pièces) | Terminé | `dev` |

---

## Cas Simples — Paiement NFC

**Cas 1 — Café servi avec solde suffisant**
```
// ETANT DONNE une machine à café avec une clé pré-payée chargée
// QUAND l'utilisateur présente sa clé au lecteur
// ALORS un café est préparé
```

**Cas 2 — Aucun café avec une clé rechargeable**
```
// ETANT DONNE une machine à café
// QUAND une clé rechargeable est présentée
// ALORS aucun café n'est préparé
```

**Cas 3 — Aucun débit avec une clé rechargeable**
```
// ETANT DONNE une machine à café
// QUAND une clé rechargeable est présentée
// ALORS aucun débit n'est tenté sur la clé
```

**Cas 4 — Aucun café quand le débit est refusé**
```
// ETANT DONNE une machine à café avec une clé pré-payée sans solde
// QUAND la clé est présentée
// ALORS aucun café n'est préparé
```

**Cas 5 — Aucun remboursement quand le solde est insuffisant**
```
// ETANT DONNE une machine à café avec une clé pré-payée sans solde
// QUAND la clé est présentée
// ALORS aucun remboursement n'est déclenché
```

**Cas 6 — Aucun débit quand aucun dispositif**
```
// ETANT DONNE une machine à café
// QUAND le lecteur signale l'absence de clé
// ALORS aucun débit n'est tenté sur la clé
```

**Cas 7 — Aucun débit au démarrage**
```
// ETANT DONNE une machine à café sans interaction
// ALORS aucun débit n'est déclenché au démarrage
```

**Cas 8 — Préparation tentée malgré une panne brewer**
```
// ETANT DONNE une machine à café avec un brewer en panne et une clé chargée
// QUAND la clé est présentée
// ALORS la préparation du café est tentée malgré la panne
```

**Cas 9 — Remboursement après panne brewer**
```
// ETANT DONNE une machine à café avec un brewer en panne et une clé chargée
// QUAND la clé est présentée et le brewer tombe en panne
// ALORS le montant débité est remboursé sur la clé
```

**Cas 10 — Aucun remboursement quand café préparé avec succès**
```
// ETANT DONNE une machine à café avec une clé chargée
// QUAND la clé est présentée et le café est préparé sans panne
// ALORS aucun remboursement n'est déclenché
```

**Cas 11 — Deux cafés pour deux présentations successives**
```
// ETANT DONNE une machine à café avec une clé pré-payée chargée
// QUAND l'utilisateur présente sa clé deux fois de suite
// ALORS deux cafés sont préparés et deux débits sont effectués
```

**Cas 12 — Aucun café pour deux présentations sans solde**
```
// ETANT DONNE une machine à café avec une clé sans solde
// QUAND l'utilisateur présente sa clé deux fois de suite
// ALORS aucun café n'est préparé et deux débits sont refusés
```

**Cas 13 — Deux remboursements pour deux présentations avec panne**
```
// ETANT DONNE une machine à café avec un brewer en panne et une clé chargée
// QUAND l'utilisateur présente sa clé deux fois de suite
// ALORS deux tentatives de café sont faites, deux débits et deux remboursements sont effectués
```

**Cas 14 — Deux cafés avec retrait entre les deux présentations**
```
// ETANT DONNE une machine à café avec une clé pré-payée chargée
// QUAND l'utilisateur présente sa clé, la retire, puis la représente
// ALORS deux cafés sont préparés et deux débits sont effectués
```

**Cas 15 — Un seul café après clé rechargeable puis clé pré-payée**
```
// ETANT DONNE une machine à café avec une clé pré-payée chargée
// QUAND une clé rechargeable est présentée puis la clé pré-payée
// ALORS un seul café est préparé et un seul débit est effectué
```

**Cas 16 — Montant débité exactement égal au prix d'un café**
```
// ETANT DONNE une machine à café avec une clé chargée
// QUAND la clé est présentée
// ALORS exactement 40 centimes sont débités
```

---

## Cas Complexes — Recharge de la clé café avec des pièces

**Cas 1.1 — Recharge standard**
```
// ETANT DONNE une machine avec un badge rechargeable posé sur le lecteur
// QUAND l'utilisateur insère une pièce de 2 €
// ALORS la clé est créditée de 2 € et la pièce est encaissée
```

**Cas 1.2 — Recharge successive de plusieurs pièces**
```
// ETANT DONNE une machine avec un badge rechargeable posé sur le lecteur
// QUAND l'utilisateur insère successivement une pièce de 2 € puis une pièce de 1 €
// ALORS deux recharges sont effectuées et deux pièces sont encaissées
```

**Cas 1.3 — Paiement d'un café par badge avec solde suffisant**
```
// ETANT DONNE une machine avec un badge pré-payé dont le solde est suffisant
// QUAND l'utilisateur présente son badge pour un café à 0,40 €
// ALORS la clé est débitée de 0,40 € et le café est distribué
```

**Cas 2.1 — Recharge jusqu'au plafond exact**
```
// ETANT DONNE une machine avec un badge rechargeable posé sur le lecteur
// QUAND l'utilisateur insère une pièce de 2 € qui atteint exactement le plafond
// ALORS la recharge est acceptée et la pièce est encaissée
```

**Cas 2.2 — Recharge dépassant le plafond**
```
// ETANT DONNE une machine avec un badge rechargeable posé sur le lecteur
// QUAND l'utilisateur insère une pièce et la recharge est refusée
// ALORS la pièce est rendue et aucune recharge n'est effectuée
```

**Cas 2.3 — Badge déjà au plafond**
```
// ETANT DONNE une machine avec un badge rechargeable posé sur le lecteur
// QUAND l'utilisateur insère une pièce et la recharge est refusée
// ALORS la pièce est rendue et aucune recharge n'est effectuée
```

**Cas 2.4 — Paiement d'un café avec solde insuffisant**
```
// ETANT DONNE une machine avec un badge dont le solde est insuffisant pour un café
// QUAND l'utilisateur présente son badge pour un café à 0,40 €
// ALORS aucun café n'est distribué et aucun remboursement n'est déclenché
```

**Cas 3.1 — Perte de connexion NFC au moment de l'insertion**
```
// ETANT DONNE une machine avec un badge rechargeable posé sur le lecteur
// QUAND l'utilisateur insère une pièce et la connexion est perdue avant la fin de l'écriture
// ALORS la pièce est rendue et aucune recharge n'est effectuée
```

**Cas 3.2 — Retrait du badge entre deux pièces**
```
// ETANT DONNE une machine avec un badge rechargeable posé sur le lecteur et une première pièce de 2 € insérée avec succès
// QUAND l'utilisateur retire son badge puis insère une pièce de 1 €
// ALORS la clé n'est créditée qu'une seule fois
```

**Cas 3.3 — Badge retiré volontairement sans insertion**
```
// ETANT DONNE une machine avec un badge rechargeable posé sur le lecteur
// QUAND l'utilisateur retire son badge sans insérer de pièce
// ALORS aucune recharge et aucun mouvement de monnaie ne sont effectués
```

**Cas 3.4 — Perte de connexion pendant le débit café**
```
// ETANT DONNE une machine avec un badge dont la connexion est perdue pendant le débit
// QUAND l'utilisateur présente son badge pour un café à 0,40 €
// ALORS aucun café n'est distribué et la machine redevient disponible sans remboursement
```

**Cas 4.1 — Double impulsion matérielle d'insertion**
```
// ETANT DONNE une machine avec un badge rechargeable posé sur le lecteur
// QUAND le système reçoit deux signaux d'insertion pour la même pièce de 2 €
// ALORS la clé n'est créditée qu'une seule fois
```