namespace MachineACafé.Test.Utilities;

internal static class ChangeMachineAssert
{
    public static void MonnaieEncaissée(MonnaieTest monnaie) =>
        Assert.Equal(1, monnaie.Encaissements);

    public static void AucunEncaissement(MonnaieTest monnaie) =>
        Assert.Equal(0, monnaie.Encaissements);

    public static void MonnaieRendue(MonnaieTest monnaie) =>
        Assert.Equal(1, monnaie.Remboursements);

    public static void AucunRemboursement(MonnaieTest monnaie) =>
        Assert.Equal(0, monnaie.Remboursements);
}
