using Hardware;
using MachineACafé.Test.Utilities;

namespace MachineACafé.Test;

public class RechargeNfcTest
{
    [Fact]
    public void RechargeEffectuée_QuandBadgePrésentEtPièceInsérée()
    {
        // ETANT DONNE une machine avec un badge rechargeable posé sur le lecteur
        var badge = new BadgeRecharge(rechargeAcceptée: true);
        var monnaie = new ChangeMachineFake();
        var monnaiespy = new ChangeMachineSpy(monnaie);
        _ = new SoftwareMachineBuilder()
            .AyantUnBadgeRecharge(badge)
            .AyantUneChangeMachine(monnaiespy)
            .Build();
        badge.PoserSurLecteur();

        // QUAND l'utilisateur insère une pièce de 2 €
        monnaie.SimulerInsertionPièce(CoinCode.TwoEuros);

        // ALORS la clé est créditée de 2 € et la pièce est encaissée
        Assert.Equal((ushort)200, badge.DernierMontantRechargé);
        Assert.Equal(1, monnaiespy.CollectStoredMoneyInvocations);
    }

    [Fact]
    public void DeuxRechargesEffectuées_QuandDeuxPiècesInsérées()
    {
        // ETANT DONNE une machine avec un badge rechargeable posé sur le lecteur
        var badge = new BadgeRecharge(rechargeAcceptée: true);
        var monnaie = new ChangeMachineFake();
        var monnaiespy = new ChangeMachineSpy(monnaie);
        _ = new SoftwareMachineBuilder()
            .AyantUnBadgeRecharge(badge)
            .AyantUneChangeMachine(monnaiespy)
            .Build();
        badge.PoserSurLecteur();

        // QUAND l'utilisateur insère successivement une pièce de 2 € puis une pièce de 1 €
        monnaie.SimulerInsertionPièce(CoinCode.TwoEuros);
        monnaie.SimulerInsertionPièce(CoinCode.OneEuro);

        // ALORS deux recharges sont effectuées et deux pièces sont encaissées
        Assert.Equal(2, badge.NombreRecharges);
        Assert.Equal(2, monnaiespy.CollectStoredMoneyInvocations);
    }

    [Fact]
    public void PièceRendue_QuandRechargeRefusée()
    {
        // ETANT DONNE une machine avec un badge rechargeable posé sur le lecteur
        var badge = new BadgeRecharge(rechargeAcceptée: false);
        var monnaie = new ChangeMachineFake();
        var monnaiespy = new ChangeMachineSpy(monnaie);
        _ = new SoftwareMachineBuilder()
            .AyantUnBadgeRecharge(badge)
            .AyantUneChangeMachine(monnaiespy)
            .Build();
        badge.PoserSurLecteur();

        // QUAND l'utilisateur insère une pièce de 2 €
        monnaie.SimulerInsertionPièce(CoinCode.TwoEuros);

        // ALORS la pièce est rendue et aucune recharge n'est effectuée
        Assert.Equal(1, monnaiespy.FlushStoredMoneyInvocations);
        Assert.Equal(0, monnaiespy.CollectStoredMoneyInvocations);
    }

    [Fact]
    public void UnSeulCrédit_QuandBadgeRetiréEntreLesDeuxPièces()
    {
        // ETANT DONNE une machine avec un badge rechargeable posé sur le lecteur et une première pièce de 2 € insérée avec succès
        var badge = new BadgeRecharge(rechargeAcceptée: true);
        var monnaie = new ChangeMachineFake();
        _ = new SoftwareMachineBuilder()
            .AyantUnBadgeRecharge(badge)
            .AyantUneChangeMachine(monnaie)
            .Build();
        badge.PoserSurLecteur();
        monnaie.SimulerInsertionPièce(CoinCode.TwoEuros);

        // QUAND l'utilisateur retire son badge puis insère une pièce de 1 €
        badge.RetirerDuLecteur();
        monnaie.SimulerInsertionPièce(CoinCode.OneEuro);

        // ALORS la clé n'est créditée qu'une seule fois
        Assert.Equal(1, badge.NombreRecharges);
    }

    [Fact]
    public void AucunEffet_QuandBadgePosépuisRetiréSansPièce()
    {
        // ETANT DONNE une machine avec un badge rechargeable posé sur le lecteur
        var badge = new BadgeRecharge();
        var monnaiespy = new ChangeMachineSpy();
        _ = new SoftwareMachineBuilder()
            .AyantUnBadgeRecharge(badge)
            .AyantUneChangeMachine(monnaiespy)
            .Build();
        badge.PoserSurLecteur();

        // QUAND l'utilisateur retire son badge sans insérer de pièce
        badge.RetirerDuLecteur();

        // ALORS aucune recharge et aucun mouvement de monnaie ne sont effectués
        Assert.Equal(0, badge.NombreRecharges);
        Assert.True(monnaiespy.Untouched);
    }

    [Fact]
    public void CaféServiEtCléDébitéeDe40Cts_QuandBadgePréPayéPrésenté()
    {
        // ETANT DONNE une machine avec un badge pré-payé dont le solde est suffisant
        var clé = new CléNfc(soldeSuffisant: true);
        var brewer = new BrewerSpy();
        _ = new SoftwareMachineBuilder()
            .AyantUneClé(clé)
            .AyantUnBrewer(brewer)
            .Build();

        // QUAND l'utilisateur présente son badge pour un café à 0,40 €
        clé.Présenter();

        // ALORS la clé est débitée de 0,40 € et le café est distribué
        Assert.Equal((ushort)40, clé.DernierMontantDébité);
        BrewerAssert.CaféPréparé(brewer);
    }

    [Fact]
    public void RechargeAcceptée_QuandPièceAtteinLePlafond()
    {
        // ETANT DONNE une machine avec un badge rechargeable posé sur le lecteur
        var badge = new BadgeRecharge(rechargeAcceptée: true);
        var monnaie = new ChangeMachineFake();
        var monnaiespy = new ChangeMachineSpy(monnaie);
        _ = new SoftwareMachineBuilder()
            .AyantUnBadgeRecharge(badge)
            .AyantUneChangeMachine(monnaiespy)
            .Build();
        badge.PoserSurLecteur();

        // QUAND l'utilisateur insère une pièce de 2 € (qui atteint exactement le plafond)
        monnaie.SimulerInsertionPièce(CoinCode.TwoEuros);

        // ALORS la recharge est acceptée et la pièce est encaissée
        Assert.Equal(1, badge.NombreRecharges);
        Assert.Equal(1, monnaiespy.CollectStoredMoneyInvocations);
    }

    [Fact]
    public void AucunCafé_QuandSoldeInsuffisantPourPayer()
    {
        // ETANT DONNE une machine avec un badge dont le solde est insuffisant pour un café
        var clé = new CléNfc(soldeSuffisant: false);
        var brewer = new BrewerSpy();
        _ = new SoftwareMachineBuilder()
            .AyantUneClé(clé)
            .AyantUnBrewer(brewer)
            .Build();

        // QUAND l'utilisateur présente son badge pour un café à 0,40 €
        clé.Présenter();

        // ALORS aucun café n'est distribué et aucun remboursement n'est déclenché
        BrewerAssert.AucunCafé(brewer);
        Assert.Equal(0, clé.NombreRemboursements);
    }

    [Fact]
    public void AucunCafé_QuandConnexionPerduePendantLeDébit()
    {
        // ETANT DONNE une machine avec un badge dont la connexion est perdue pendant le débit
        var clé = new CléNfc(soldeSuffisant: false);
        var brewer = new BrewerSpy();
        _ = new SoftwareMachineBuilder()
            .AyantUneClé(clé)
            .AyantUnBrewer(brewer)
            .Build();

        // QUAND l'utilisateur présente son badge et la connexion est perdue pendant le débit
        clé.Présenter();

        // ALORS aucun café n'est distribué et la machine redevient disponible sans remboursement
        BrewerAssert.AucunCafé(brewer);
        Assert.Equal(0, clé.NombreRemboursements);
    }

    [Fact]
    public void UnSeulCrédit_QuandDoubleSignalPourLaMêmePièce()
    {
        // ETANT DONNE une machine avec un badge rechargeable posé sur le lecteur
        var badge = new BadgeRecharge(rechargeAcceptée: true);
        var monnaie = new ChangeMachineFake();
        _ = new SoftwareMachineBuilder()
            .AyantUnBadgeRecharge(badge)
            .AyantUneChangeMachine(monnaie)
            .Build();
        badge.PoserSurLecteur();

        // QUAND le système reçoit deux signaux d'insertion pour la même pièce de 2 €
        monnaie.SimulerInsertionPièce(CoinCode.TwoEuros);
        monnaie.SimulerInsertionPièce(CoinCode.TwoEuros);

        // ALORS la clé n'est créditée qu'une seule fois
        Assert.Equal(1, badge.NombreRecharges);
    }
}
