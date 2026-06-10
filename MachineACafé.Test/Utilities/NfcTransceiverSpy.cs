using Hardware;

namespace MachineACafé.Test.Utilities;

internal class NfcTransceiverSpy : INfcTransceiver
{
    private readonly INfcTransceiver _behavior;

    public ushort TryChargeAmountInvocations { get; private set; }
    public ushort? LastAmountCharged { get; private set; }
    public bool Untouched => TryChargeAmountInvocations == 0;

    public NfcTransceiverSpy() : this(new NfcTransceiverStub()) { }

    public NfcTransceiverSpy(INfcTransceiver behavior)
    {
        _behavior = behavior;
    }

    public event Action<NfcState> NfcStateChanged
    {
        add => _behavior.NfcStateChanged += value;
        remove => _behavior.NfcStateChanged -= value;
    }

    public bool TryChargeAmount(ushort amountInCents)
    {
        TryChargeAmountInvocations++;
        LastAmountCharged = amountInCents;
        return _behavior.TryChargeAmount(amountInCents);
    }

    public bool TryRefillDevice(ushort amountInCents) => _behavior.TryRefillDevice(amountInCents);
}
