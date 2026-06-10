using Hardware;

namespace MachineACafé.Test.Utilities;

internal class NfcTransceiverFake : INfcTransceiver
{
    private Action<NfcState>? _handler;
    private readonly bool _chargeResult;

    public NfcTransceiverFake(bool chargeResult = true)
    {
        _chargeResult = chargeResult;
    }

    public event Action<NfcState> NfcStateChanged
    {
        add => _handler += value;
        remove => _handler -= value;
    }

    public bool TryChargeAmount(ushort amountInCents) => _chargeResult;

    public bool TryRefillDevice(ushort amountInCents) => false;

    public void SimulerApparitionCle(NfcState état) => _handler?.Invoke(état);
}
