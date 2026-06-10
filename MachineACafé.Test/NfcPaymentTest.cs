using Hardware;
using MachineACafé.Test.Utilities;

namespace MachineACafé.Test;

public class NfcPaymentTest
{
    [Fact]
    public void CaféServi_QuandCléNfcPrépayéeEtSoldeSuffisant()
    {
        // ETANT DONNE une machine à café avec une clé NFC ayant un solde suffisant
        var nfc = new NfcTransceiverFake(chargeResult: true);
        var brewer = new BrewerSpy();
        _ = new SoftwareMachineBuilder()
            .AyantUnNfcTransceiver(nfc)
            .AyantUnBrewer(brewer)
            .Build();

        // QUAND une clé pré-payée est présentée
        nfc.SimulerApparitionCle(NfcState.PrepaidDevicePresent);

        // ALORS MakeACoffee est appelé une fois
        BrewerAssert.CaféPréparé(brewer);
    }

    [Fact]
    public void CaféNonServi_QuandCléRechargeable()
    {
        // ETANT DONNE une machine à café
        var nfc = new NfcTransceiverFake(chargeResult: true);
        var brewer = new BrewerSpy();
        _ = new SoftwareMachineBuilder()
            .AyantUnNfcTransceiver(nfc)
            .AyantUnBrewer(brewer)
            .Build();

        // QUAND une clé rechargeable (non pré-payée) est présentée
        nfc.SimulerApparitionCle(NfcState.RefillableDevicePresent);

        // ALORS aucun café n'est préparé et TryChargeAmount n'est pas appelé
        BrewerAssert.AucunCafé(brewer);
    }

    [Fact]
    public void CaféNonServi_QuandSoldeInsuffisant()
    {
        // ETANT DONNE une machine à café avec une clé NFC sans solde suffisant
        var nfc = new NfcTransceiverFake(chargeResult: false);
        var brewer = new BrewerSpy();
        _ = new SoftwareMachineBuilder()
            .AyantUnNfcTransceiver(nfc)
            .AyantUnBrewer(brewer)
            .Build();

        // QUAND la clé est présentée mais le débit échoue
        nfc.SimulerApparitionCle(NfcState.PrepaidDevicePresent);

        // ALORS aucun café n'est préparé
        BrewerAssert.AucunCafé(brewer);
    }

    [Fact]
    public void TryChargeAmountNonAppelé_QuandAucunDispositif()
    {
        // ETANT DONNE une machine à café avec un espion NFC
        var nfc = new NfcTransceiverFake(chargeResult: true);
        var spy = new NfcTransceiverSpy(nfc);
        _ = new SoftwareMachineBuilder()
            .AyantUnNfcTransceiver(spy)
            .Build();

        // QUAND l'événement NoDevice est reçu (disparition ou absence de clé)
        nfc.SimulerApparitionCle(NfcState.NoDevice);

        // ALORS TryChargeAmount n'est jamais tenté
        Assert.True(spy.Untouched);
    }

    [Fact]
    public void TryChargeAmountNonAppelé_QuandMachineAuRepos()
    {
        // ETANT DONNE une machine à café avec un espion NFC, sans interaction
        var nfc = new NfcTransceiverFake(chargeResult: true);
        var spy = new NfcTransceiverSpy(nfc);
        _ = new SoftwareMachineBuilder()
            .AyantUnNfcTransceiver(spy)
            .Build();

        // ALORS TryChargeAmount n'est jamais appelé au démarrage
        Assert.True(spy.Untouched);
    }

    [Fact]
    public void CaféNonServi_QuandBrewerDéfaillantEtCléNfc()
    {
        // ETANT DONNE une machine à café avec un brewer défaillant et une clé avec solde suffisant
        var nfc = new NfcTransceiverFake(chargeResult: true);
        var brewer = new BrewerSpy(new BrewerDummy());
        _ = new SoftwareMachineBuilder()
            .AyantUnNfcTransceiver(nfc)
            .AyantUnBrewer(brewer)
            .Build();

        // QUAND une clé pré-payée est présentée
        nfc.SimulerApparitionCle(NfcState.PrepaidDevicePresent);

        // ALORS MakeACoffee est tenté une fois (le débit NFC a déjà eu lieu)
        Assert.Equal(1, brewer.MakeACoffeeInvocations);
    }

    [Fact]
    public void ArgentRemboursé_QuandBrewerDéfaillantAprèsDébitNfc()
    {
        // ETANT DONNE une machine à café avec un brewer défaillant et un espion NFC
        var nfc = new NfcTransceiverFake(chargeResult: true);
        var spy = new NfcTransceiverSpy(nfc);
        _ = new SoftwareMachineBuilder()
            .AyantUnNfcTransceiver(spy)
            .AyantUnBrewer(new BrewerDummy())
            .Build();

        // QUAND une clé pré-payée est présentée et le brewer tombe en panne
        nfc.SimulerApparitionCle(NfcState.PrepaidDevicePresent);

        // ALORS TryRefillDevice est appelé avec 40 centimes pour rembourser la clé
        Assert.Equal(1, spy.TryRefillDeviceInvocations);
        Assert.Equal((ushort)40, spy.LastAmountRefilled);
    }

    [Fact]
    public void TryChargeAmountAppeléAvecExactement40Centimes()
    {
        // ETANT DONNE une machine à café avec un espion NFC et une clé avec solde suffisant
        var nfc = new NfcTransceiverFake(chargeResult: true);
        var spy = new NfcTransceiverSpy(nfc);
        _ = new SoftwareMachineBuilder()
            .AyantUnNfcTransceiver(spy)
            .Build();

        // QUAND une clé pré-payée est présentée
        nfc.SimulerApparitionCle(NfcState.PrepaidDevicePresent);

        // ALORS le débit est de 40 centimes exactement (prix d'un café)
        Assert.Equal((ushort)40, spy.LastAmountCharged);
    }
}
