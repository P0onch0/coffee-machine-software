using Hardware;

namespace MachineACafé.Test.Utilities;

internal class BadgeRechargeFake
{
    private readonly NfcTransceiverFake _fake;
    private readonly NfcTransceiverSpy _spy;

    public INfcTransceiver Transceiver => _spy;
    public int NombreRecharges => _spy.TryRefillDeviceInvocations;
    public ushort? DernierMontantRechargé => _spy.LastAmountRefilled;

    public BadgeRechargeFake(bool rechargeAcceptée = true)
    {
        _fake = new NfcTransceiverFake(chargeResult: false, refillResult: rechargeAcceptée);
        _spy = new NfcTransceiverSpy(_fake);
    }

    public void PoserSurLecteur() => _fake.SimulerApparitionCle(NfcState.RefillableDevicePresent);
    public void RetirerDuLecteur() => _fake.SimulerApparitionCle(NfcState.NoDevice);
}
