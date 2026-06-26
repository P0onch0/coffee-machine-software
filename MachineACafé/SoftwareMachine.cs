using Hardware;

namespace MachineACafé;

public class SoftwareMachine
{
    public const ushort PrixCafé = 40;

    private readonly IBrewer _brewer;
    private readonly IChangeMachine _changeMachine;
    private readonly INfcTransceiver _nfcTransceiver;
    private bool _modeRecharge = false;
    private ushort? _dernierCentimes = null;

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
        _modeRecharge = état == NfcState.RefillableDevicePresent;
        _dernierCentimes = null;
        if (état != NfcState.PrepaidDevicePresent) return;
        if (!_nfcTransceiver.TryChargeAmount(PrixCafé)) return;
        try { _brewer.MakeACoffee(); } catch { _nfcTransceiver.TryRefillDevice(PrixCafé); }
    }

    private void Insérer(Coin somme)
    {
        if (_modeRecharge)
        {
            if (somme.ValueInCents == _dernierCentimes) { _dernierCentimes = null; return; }
            _dernierCentimes = somme.ValueInCents;
            bool rechargé = _nfcTransceiver.TryRefillDevice(somme.ValueInCents);
            if (rechargé) _changeMachine.CollectStoredMoney();
            else _changeMachine.FlushStoredMoney();
            return;
        }

        if (somme.ValueInCents < PrixCafé)
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