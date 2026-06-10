namespace MachineACafé.Test.Utilities;

internal static class BrewerAssert
{
    public static void CaféPréparé(BrewerSpy brewer) =>
        Assert.Equal(1, brewer.MakeACoffeeInvocations);

    public static void AucunCafé(BrewerSpy brewer) =>
        Assert.Equal(0, brewer.MakeACoffeeInvocations);
}
