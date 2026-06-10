using Hardware;

namespace MachineACafé.Test.Utilities;

internal class CléNfc
{
    private readonly NfcTransceiverFake _fake;
    private readonly NfcTransceiverSpy _spy;

    public INfcTransceiver Transceiver => _spy;
    public int NombreDébits => _spy.TryChargeAmountInvocations;
    public int NombreRemboursements => _spy.TryRefillDeviceInvocations;
    public ushort? DernierMontantDébité => _spy.LastAmountCharged;
    public ushort? DernierMontantRemboursé => _spy.LastAmountRefilled;

    public CléNfc(bool soldeSuffisant = true)
    {
        _fake = new NfcTransceiverFake(chargeResult: soldeSuffisant);
        _spy = new NfcTransceiverSpy(_fake);
    }

    public void Présenter() => _fake.SimulerApparitionCle(NfcState.PrepaidDevicePresent);
    public void PrésentationCléRechargeable() => _fake.SimulerApparitionCle(NfcState.RefillableDevicePresent);
    public void Retirer() => _fake.SimulerApparitionCle(NfcState.NoDevice);
}
