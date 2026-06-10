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
            .AyantUnNfcTransceiver(clé.Transceiver)
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
            .AyantUnNfcTransceiver(clé.Transceiver)
            .AyantUnBrewer(brewer)
            .Build();

        // QUAND une clé rechargeable est présentée (non pré-payée)
        clé.PrésentationCléRechargeable();

        // ALORS aucun café n'est préparé
        BrewerAssert.AucunCafé(brewer);
    }

    [Fact]
    public void CaféNonServi_QuandDébitRefusé()
    {
        // ETANT DONNE une machine à café avec une clé pré-payée sans solde
        var clé = new CléNfc(soldeSuffisant: false);
        var brewer = new BrewerSpy();
        _ = new SoftwareMachineBuilder()
            .AyantUnNfcTransceiver(clé.Transceiver)
            .AyantUnBrewer(brewer)
            .Build();

        // QUAND la clé est présentée
        clé.Présenter();

        // ALORS aucun café n'est préparé
        BrewerAssert.AucunCafé(brewer);
    }

    [Fact]
    public void AucunDébit_QuandAucunDispositif()
    {
        // ETANT DONNE une machine à café
        var clé = new CléNfc();
        _ = new SoftwareMachineBuilder()
            .AyantUnNfcTransceiver(clé.Transceiver)
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
            .AyantUnNfcTransceiver(clé.Transceiver)
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
            .AyantUnNfcTransceiver(clé.Transceiver)
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
            .AyantUnNfcTransceiver(clé.Transceiver)
            .AyantUnBrewer(new BrewerDummy())
            .Build();

        // QUAND la clé est présentée et le brewer tombe en panne
        clé.Présenter();

        // ALORS le montant débité est remboursé sur la clé
        Assert.Equal(1, clé.NombreRemboursements);
        Assert.Equal((ushort)40, clé.DernierMontantRemboursé);
    }

    [Fact]
    public void MontantDébité_EstExactementLePrixDUnCafé()
    {
        // ETANT DONNE une machine à café avec une clé chargée
        var clé = new CléNfc(soldeSuffisant: true);
        _ = new SoftwareMachineBuilder()
            .AyantUnNfcTransceiver(clé.Transceiver)
            .Build();

        // QUAND la clé est présentée
        clé.Présenter();

        // ALORS exactement 40 centimes sont débités
        Assert.Equal((ushort)40, clé.DernierMontantDébité);
    }
}
