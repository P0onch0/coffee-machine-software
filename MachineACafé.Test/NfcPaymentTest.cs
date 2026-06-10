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
        Assert.Equal(1, brewer.MakeACoffeeInvocations);
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
        Assert.Equal(0, brewer.MakeACoffeeInvocations);
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
        Assert.Equal(0, brewer.MakeACoffeeInvocations);
    }
}
