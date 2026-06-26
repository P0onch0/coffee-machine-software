using Hardware;

namespace MachineACafé.Test.Utilities;

internal class SoftwareMachineBuilder
{
    private IBrewer _brewer = new BrewerStub();
    private IChangeMachine _changeMachine = new ChangeMachineStub();
    private INfcTransceiver _nfcTransceiver = new NfcTransceiverStub();

    public SoftwareMachine Build()
    {
        return new SoftwareMachine(_brewer, _changeMachine, _nfcTransceiver);
    }

    public SoftwareMachineBuilder AyantUnBrewer(IBrewer brewer)
    {
        _brewer = brewer;
        return this;
    }

    public SoftwareMachineBuilder AyantUneChangeMachine(IChangeMachine changeMachine)
    {
        _changeMachine = changeMachine;
        return this;
    }

    public SoftwareMachineBuilder AyantUnNfcTransceiver(INfcTransceiver nfcTransceiver)
    {
        _nfcTransceiver = nfcTransceiver;
        return this;
    }

    public SoftwareMachineBuilder AyantUneClé(CléNfcFake clé)
    {
        _nfcTransceiver = clé.Transceiver;
        return this;
    }

    public SoftwareMachineBuilder AyantUneMonnaie(MonnaieTest monnaie)
    {
        _changeMachine = monnaie.ChangeMachine;
        return this;
    }

    public SoftwareMachineBuilder AyantUnBadgeRecharge(BadgeRechargeFake badge)
    {
        _nfcTransceiver = badge.Transceiver;
        return this;
    }
}