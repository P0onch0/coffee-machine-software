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
}
