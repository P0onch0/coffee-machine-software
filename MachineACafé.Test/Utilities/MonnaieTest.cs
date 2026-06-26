using Hardware;

namespace MachineACafé.Test.Utilities;

internal class MonnaieTest
{
    private readonly ChangeMachineFake _fake = new();
    private readonly ChangeMachineSpy _spy;

    public IChangeMachine ChangeMachine => _spy;
    public ushort Encaissements => _spy.CollectStoredMoneyInvocations;
    public ushort Remboursements => _spy.FlushStoredMoneyInvocations;
    public bool Untouched => _spy.Untouched;

    public MonnaieTest()
    {
        _spy = new ChangeMachineSpy(_fake);
    }

    public void InsérerPièce(CoinCode code) => _fake.SimulerInsertionPièce(code);
}
