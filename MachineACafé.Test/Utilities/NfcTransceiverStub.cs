using Hardware;

namespace MachineACafé.Test.Utilities;

internal class NfcTransceiverStub : INfcTransceiver
{
    public event Action<NfcState> NfcStateChanged { add { } remove { } }

    public bool TryChargeAmount(ushort amountInCents) => false;

    public bool TryRefillDevice(ushort amountInCents) => false;
}
