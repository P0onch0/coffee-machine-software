using Hardware;

namespace MachineACafé;

public class SoftwareMachine
{
    private readonly IBrewer _brewer;
    private readonly IChangeMachine _changeMachine;
    private readonly INfcTransceiver _nfcTransceiver;

    public SoftwareMachine(IBrewer brewer, IChangeMachine changeMachine, INfcTransceiver nfcTransceiver)
    {
        _brewer = brewer;
        _changeMachine = changeMachine;
        _nfcTransceiver = nfcTransceiver;
        _changeMachine.RegisterMoneyInsertedCallback(coin => Insérer(new Coin((ushort) coin)));
        _nfcTransceiver.NfcStateChanged += PaiementNfc;
    }

    private void PaiementNfc(NfcState état)
    {
        _nfcTransceiver.TryChargeAmount(40);
        _brewer.MakeACoffee();
    }

    private void Insérer(Coin somme)
    {
        if (somme.ValueInCents < 40)
        {
            _changeMachine.FlushStoredMoney();
            return;
        }

        try
        {
            _brewer.MakeACoffee();
            _changeMachine.CollectStoredMoney();
        }
        catch
        {
            _changeMachine.FlushStoredMoney();
        }
    }
}