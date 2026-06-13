using Hardware;

namespace MachineACafé.Test.Utilities;

internal class NfcTransceiverFake : INfcTransceiver
{
    private Action<NfcState>? _handler;
    private readonly bool _chargeResult;
    private readonly bool _refillResult;

    public NfcTransceiverFake(bool chargeResult = true, bool refillResult = false)
    {
        _chargeResult = chargeResult;
        _refillResult = refillResult;
    }

    public event Action<NfcState> NfcStateChanged
    {
        add => _handler += value;
        remove => _handler -= value;
    }

    public bool TryChargeAmount(ushort amountInCents) => _chargeResult;

    public bool TryRefillDevice(ushort amountInCents) => _refillResult;

    public void SimulerApparitionCle(NfcState état) => _handler?.Invoke(état);
}
