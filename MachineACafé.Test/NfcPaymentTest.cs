using MachineACafé.Test.Utilities;

namespace MachineACafé.Test;

public class NfcPaymentTest
{
    [Fact]
    public void CaféServi_QuandSoldeSuffisant()
    {
        // ETANT DONNE une machine à café avec une clé pré-payée chargée
        var clé = new CléNfc(soldeSuffisant: true);
        var brewer = new BrewerSpy();
        _ = new SoftwareMachineBuilder()
            .AyantUneClé(clé)
            .AyantUnBrewer(brewer)
            .Build();

        // QUAND l'utilisateur présente sa clé au lecteur
        clé.Présenter();

        // ALORS un café est préparé
        BrewerAssert.CaféPréparé(brewer);
    }

    [Fact]
    public void CaféNonServi_QuandCléRechargeable()
    {
        // ETANT DONNE une machine à café
        var clé = new CléNfc();
        var brewer = new BrewerSpy();
        _ = new SoftwareMachineBuilder()
            .AyantUneClé(clé)
            .AyantUnBrewer(brewer)
            .Build();

        // QUAND une clé rechargeable est présentée (non pré-payée)
        clé.PrésentationCléRechargeable();

        // ALORS aucun café n'est préparé
        BrewerAssert.AucunCafé(brewer);
    }

    [Fact]
    public void AucunDébit_QuandCléRechargeable()
    {
        // ETANT DONNE une machine à café
        var clé = new CléNfc();
        _ = new SoftwareMachineBuilder()
            .AyantUneClé(clé)
            .Build();

        // QUAND une clé rechargeable est présentée
        clé.PrésentationCléRechargeable();

        // ALORS aucun débit n'est tenté sur la clé
        Assert.Equal(0, clé.NombreDébits);
    }

    [Fact]
    public void CaféNonServi_QuandDébitRefusé()
    {
        // ETANT DONNE une machine à café avec une clé pré-payée sans solde
        var clé = new CléNfc(soldeSuffisant: false);
        var brewer = new BrewerSpy();
        _ = new SoftwareMachineBuilder()
            .AyantUneClé(clé)
            .AyantUnBrewer(brewer)
            .Build();

        // QUAND la clé est présentée
        clé.Présenter();

        // ALORS aucun café n'est préparé
        BrewerAssert.AucunCafé(brewer);
    }

    [Fact]
    public void AucunRemboursement_QuandSoldeInsuffisant()
    {
        // ETANT DONNE une machine à café avec une clé pré-payée sans solde
        var clé = new CléNfc(soldeSuffisant: false);
        _ = new SoftwareMachineBuilder()
            .AyantUneClé(clé)
            .Build();

        // QUAND la clé est présentée
        clé.Présenter();

        // ALORS aucun remboursement n'est déclenché
        Assert.Equal(0, clé.NombreRemboursements);
    }

    [Fact]
    public void AucunDébit_QuandAucunDispositif()
    {
        // ETANT DONNE une machine à café
        var clé = new CléNfc();
        _ = new SoftwareMachineBuilder()
            .AyantUneClé(clé)
            .Build();

        // QUAND le lecteur signale l'absence de clé
        clé.Retirer();

        // ALORS aucun débit n'est tenté sur la clé
        Assert.Equal(0, clé.NombreDébits);
    }

    [Fact]
    public void AucunDébit_QuandMachineAuRepos()
    {
        // ETANT DONNE une machine à café sans interaction
        var clé = new CléNfc();
        _ = new SoftwareMachineBuilder()
            .AyantUneClé(clé)
            .Build();

        // ALORS aucun débit n'est déclenché au démarrage
        Assert.Equal(0, clé.NombreDébits);
    }

    [Fact]
    public void PréparationTentée_QuandBrewerEnPanneAprèsDébit()
    {
        // ETANT DONNE une machine à café avec un brewer en panne et une clé chargée
        var clé = new CléNfc(soldeSuffisant: true);
        var brewer = new BrewerSpy(new BrewerDummy());
        _ = new SoftwareMachineBuilder()
            .AyantUneClé(clé)
            .AyantUnBrewer(brewer)
            .Build();

        // QUAND la clé est présentée
        clé.Présenter();

        // ALORS la préparation du café est tentée malgré la panne
        Assert.Equal(1, brewer.MakeACoffeeInvocations);
    }

    [Fact]
    public void ArgentRemboursé_QuandBrewerEnPanneAprèsDébit()
    {
        // ETANT DONNE une machine à café avec un brewer en panne et une clé chargée
        var clé = new CléNfc(soldeSuffisant: true);
        _ = new SoftwareMachineBuilder()
            .AyantUneClé(clé)
            .AyantUnBrewer(new BrewerDummy())
            .Build();

        // QUAND la clé est présentée et le brewer tombe en panne
        clé.Présenter();

        // ALORS le montant débité est remboursé sur la clé
        Assert.Equal(1, clé.NombreRemboursements);
        Assert.Equal((ushort)40, clé.DernierMontantRemboursé);
    }

    [Fact]
    public void AucunRemboursement_QuandCaféPréparéAvecSuccès()
    {
        // ETANT DONNE une machine à café avec une clé chargée
        var clé = new CléNfc(soldeSuffisant: true);
        _ = new SoftwareMachineBuilder()
            .AyantUneClé(clé)
            .Build();

        // QUAND la clé est présentée et le café est préparé sans panne
        clé.Présenter();

        // ALORS aucun remboursement n'est déclenché
        Assert.Equal(0, clé.NombreRemboursements);
    }

    [Fact]
    public void DeuxCafésPréparés_QuandCléPrésentéeDeuxFoisDeSuite()
    {
        // ETANT DONNE une machine à café avec une clé pré-payée chargée
        var clé = new CléNfc(soldeSuffisant: true);
        var brewer = new BrewerSpy();
        _ = new SoftwareMachineBuilder()
            .AyantUneClé(clé)
            .AyantUnBrewer(brewer)
            .Build();

        // QUAND l'utilisateur présente sa clé deux fois de suite
        clé.Présenter();
        clé.Présenter();

        // ALORS deux cafés sont préparés et deux débits sont effectués
        Assert.Equal(2, brewer.MakeACoffeeInvocations);
        Assert.Equal(2, clé.NombreDébits);
    }

    [Fact]
    public void AucunCafé_QuandDeuxPrésentationsEtSoldeInsuffisant()
    {
        // ETANT DONNE une machine à café avec une clé sans solde
        var clé = new CléNfc(soldeSuffisant: false);
        var brewer = new BrewerSpy();
        _ = new SoftwareMachineBuilder()
            .AyantUneClé(clé)
            .AyantUnBrewer(brewer)
            .Build();

        // QUAND l'utilisateur présente sa clé deux fois de suite
        clé.Présenter();
        clé.Présenter();

        // ALORS aucun café n'est préparé et deux débits sont refusés
        BrewerAssert.AucunCafé(brewer);
        Assert.Equal(2, clé.NombreDébits);
    }

    [Fact]
    public void DeuxRemboursements_QuandDeuxPrésentationsAvecBrewerEnPanne()
    {
        // ETANT DONNE une machine à café avec un brewer en panne et une clé chargée
        var clé = new CléNfc(soldeSuffisant: true);
        var brewer = new BrewerSpy(new BrewerDummy());
        _ = new SoftwareMachineBuilder()
            .AyantUneClé(clé)
            .AyantUnBrewer(brewer)
            .Build();

        // QUAND l'utilisateur présente sa clé deux fois de suite
        clé.Présenter();
        clé.Présenter();

        // ALORS deux tentatives de café sont faites, deux débits et deux remboursements
        Assert.Equal(2, brewer.MakeACoffeeInvocations);
        Assert.Equal(2, clé.NombreDébits);
        Assert.Equal(2, clé.NombreRemboursements);
    }

    [Fact]
    public void DeuxCafésPréparés_QuandRetraitEntreLesDeuxPrésentations()
    {
        // ETANT DONNE une machine à café avec une clé pré-payée chargée
        var clé = new CléNfc(soldeSuffisant: true);
        var brewer = new BrewerSpy();
        _ = new SoftwareMachineBuilder()
            .AyantUneClé(clé)
            .AyantUnBrewer(brewer)
            .Build();

        // QUAND l'utilisateur présente sa clé, la retire, puis la représente
        clé.Présenter();
        clé.Retirer();
        clé.Présenter();

        // ALORS deux cafés sont préparés et deux débits sont effectués
        Assert.Equal(2, brewer.MakeACoffeeInvocations);
        Assert.Equal(2, clé.NombreDébits);
    }

    [Fact]
    public void UnCafé_QuandCléRechargeableAvantCléPréPayée()
    {
        // ETANT DONNE une machine à café avec une clé pré-payée chargée
        var clé = new CléNfc(soldeSuffisant: true);
        var brewer = new BrewerSpy();
        _ = new SoftwareMachineBuilder()
            .AyantUneClé(clé)
            .AyantUnBrewer(brewer)
            .Build();

        // QUAND une clé rechargeable est présentée puis la clé pré-payée
        clé.PrésentationCléRechargeable();
        clé.Présenter();

        // ALORS un seul café est préparé et un seul débit est effectué
        Assert.Equal(1, brewer.MakeACoffeeInvocations);
        Assert.Equal(1, clé.NombreDébits);
    }

    [Fact]
    public void MontantDébité_EstExactementLePrixDUnCafé()
    {
        // ETANT DONNE une machine à café avec une clé chargée
        var clé = new CléNfc(soldeSuffisant: true);
        _ = new SoftwareMachineBuilder()
            .AyantUneClé(clé)
            .Build();

        // QUAND la clé est présentée
        clé.Présenter();

        // ALORS exactement 40 centimes sont débités
        Assert.Equal((ushort)40, clé.DernierMontantDébité);
    }
}
