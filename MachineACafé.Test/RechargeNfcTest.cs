using Hardware;
using MachineACafé.Test.Utilities;

namespace MachineACafé.Test;

public class RechargeNfcTest
{
    [Theory]
    [InlineData(CoinCode.TwentyCents)]
    [InlineData(CoinCode.FiftyCents)]
    [InlineData(CoinCode.OneEuro)]
    [InlineData(CoinCode.TwoEuros)]
    public void RechargeEffectuée_QuandBadgePrésentEtPièceInsérée(CoinCode pièce)
    {
        // ETANT DONNE une machine avec un badge rechargeable posé sur le lecteur
        var badge = new BadgeRechargeFake(rechargeAcceptée: true);
        var monnaie = new MonnaieTest();
        _ = new SoftwareMachineBuilder()
            .AyantUnBadgeRecharge(badge)
            .AyantUneMonnaie(monnaie)
            .Build();
        badge.PoserSurLecteur();

        // QUAND l'utilisateur insère une pièce
        monnaie.InsérerPièce(pièce);

        // ALORS la clé est créditée du montant de la pièce et celle-ci est encaissée
        Assert.Equal((ushort)pièce, badge.DernierMontantRechargé);
        ChangeMachineAssert.MonnaieEncaissée(monnaie);
    }

    [Fact]
    public void DeuxRechargesEffectuées_QuandDeuxPiècesInsérées()
    {
        // ETANT DONNE une machine avec un badge rechargeable posé sur le lecteur
        var badge = new BadgeRechargeFake(rechargeAcceptée: true);
        var monnaie = new MonnaieTest();
        _ = new SoftwareMachineBuilder()
            .AyantUnBadgeRecharge(badge)
            .AyantUneMonnaie(monnaie)
            .Build();
        badge.PoserSurLecteur();

        // QUAND l'utilisateur insère successivement une pièce de 2 € puis une pièce de 1 €
        monnaie.InsérerPièce(CoinCode.TwoEuros);
        monnaie.InsérerPièce(CoinCode.OneEuro);

        // ALORS deux recharges sont effectuées et deux pièces sont encaissées
        Assert.Equal(2, badge.NombreRecharges);
        Assert.Equal(2, monnaie.Encaissements);
    }

    [Theory]
    [InlineData(CoinCode.TwentyCents)]
    [InlineData(CoinCode.FiftyCents)]
    [InlineData(CoinCode.OneEuro)]
    [InlineData(CoinCode.TwoEuros)]
    public void PièceRendue_QuandRechargeRefusée(CoinCode pièce)
    {
        // ETANT DONNE une machine avec un badge rechargeable posé sur le lecteur
        var badge = new BadgeRechargeFake(rechargeAcceptée: false);
        var monnaie = new MonnaieTest();
        _ = new SoftwareMachineBuilder()
            .AyantUnBadgeRecharge(badge)
            .AyantUneMonnaie(monnaie)
            .Build();
        badge.PoserSurLecteur();

        // QUAND l'utilisateur insère une pièce
        monnaie.InsérerPièce(pièce);

        // ALORS la pièce est rendue et aucune recharge n'est effectuée
        ChangeMachineAssert.MonnaieRendue(monnaie);
        ChangeMachineAssert.AucunEncaissement(monnaie);
    }

    [Fact]
    public void UnSeulCrédit_QuandBadgeRetiréEntreLesDeuxPièces()
    {
        // ETANT DONNE une machine avec un badge rechargeable posé sur le lecteur et une première pièce de 2 € insérée avec succès
        var badge = new BadgeRechargeFake(rechargeAcceptée: true);
        var monnaie = new MonnaieTest();
        _ = new SoftwareMachineBuilder()
            .AyantUnBadgeRecharge(badge)
            .AyantUneMonnaie(monnaie)
            .Build();
        badge.PoserSurLecteur();
        monnaie.InsérerPièce(CoinCode.TwoEuros);

        // QUAND l'utilisateur retire son badge puis insère une pièce de 1 €
        badge.RetirerDuLecteur();
        monnaie.InsérerPièce(CoinCode.OneEuro);

        // ALORS la clé n'est créditée qu'une seule fois
        Assert.Equal(1, badge.NombreRecharges);
    }

    [Fact]
    public void AucunEffet_QuandBadgePosépuisRetiréSansPièce()
    {
        // ETANT DONNE une machine avec un badge rechargeable posé sur le lecteur
        var badge = new BadgeRechargeFake();
        var monnaie = new MonnaieTest();
        _ = new SoftwareMachineBuilder()
            .AyantUnBadgeRecharge(badge)
            .AyantUneMonnaie(monnaie)
            .Build();
        badge.PoserSurLecteur();

        // QUAND l'utilisateur retire son badge sans insérer de pièce
        badge.RetirerDuLecteur();

        // ALORS aucune recharge et aucun mouvement de monnaie ne sont effectués
        Assert.Equal(0, badge.NombreRecharges);
        Assert.True(monnaie.Untouched);
    }

    [Fact]
    public void CaféServiEtCléDébitée_QuandBadgePréPayéPrésenté()
    {
        // ETANT DONNE une machine avec un badge pré-payé dont le solde est suffisant
        var clé = new CléNfcFake(soldeSuffisant: true);
        var brewer = new BrewerSpy();
        _ = new SoftwareMachineBuilder()
            .AyantUneClé(clé)
            .AyantUnBrewer(brewer)
            .Build();

        // QUAND l'utilisateur présente son badge
        clé.Présenter();

        // ALORS la clé est débitée du prix du café et le café est distribué
        Assert.Equal(SoftwareMachine.PrixCafé, clé.DernierMontantDébité);
        BrewerAssert.CaféPréparé(brewer);
    }

    [Fact]
    public void RechargeAcceptée_QuandPièceAtteinLePlafond()
    {
        // ETANT DONNE une machine avec un badge rechargeable posé sur le lecteur
        var badge = new BadgeRechargeFake(rechargeAcceptée: true);
        var monnaie = new MonnaieTest();
        _ = new SoftwareMachineBuilder()
            .AyantUnBadgeRecharge(badge)
            .AyantUneMonnaie(monnaie)
            .Build();
        badge.PoserSurLecteur();

        // QUAND l'utilisateur insère une pièce de 2 € (valeur maximale)
        monnaie.InsérerPièce(CoinCode.TwoEuros);

        // ALORS la recharge est acceptée et la pièce est encaissée
        Assert.Equal(1, badge.NombreRecharges);
        ChangeMachineAssert.MonnaieEncaissée(monnaie);
    }

    [Fact]
    public void AucunCafé_QuandSoldeInsuffisantPourPayer()
    {
        // ETANT DONNE une machine avec un badge dont le solde est insuffisant pour un café
        var clé = new CléNfcFake(soldeSuffisant: false);
        var brewer = new BrewerSpy();
        _ = new SoftwareMachineBuilder()
            .AyantUneClé(clé)
            .AyantUnBrewer(brewer)
            .Build();

        // QUAND l'utilisateur présente son badge
        clé.Présenter();

        // ALORS aucun café n'est distribué et aucun remboursement n'est déclenché
        BrewerAssert.AucunCafé(brewer);
        Assert.Equal(0, clé.NombreRemboursements);
    }

    [Fact]
    public void AucunCafé_QuandConnexionPerduePendantLeDébit()
    {
        // ETANT DONNE une machine avec un badge dont la connexion est perdue pendant le débit
        var clé = new CléNfcFake(soldeSuffisant: false);
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
        var badge = new BadgeRechargeFake(rechargeAcceptée: true);
        var monnaie = new MonnaieTest();
        _ = new SoftwareMachineBuilder()
            .AyantUnBadgeRecharge(badge)
            .AyantUneMonnaie(monnaie)
            .Build();
        badge.PoserSurLecteur();

        // QUAND le système reçoit deux signaux d'insertion pour la même pièce de 2 €
        monnaie.InsérerPièce(CoinCode.TwoEuros);
        monnaie.InsérerPièce(CoinCode.TwoEuros);

        // ALORS la clé n'est créditée qu'une seule fois
        Assert.Equal(1, badge.NombreRecharges);
    }
}
