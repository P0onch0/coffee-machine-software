using Hardware;
using MachineACafé.Test.Utilities;

namespace MachineACafé.Test;

public class SoftwareMachineTest
{
    [Fact]
    public void AucuneAction()
    {
        // ETANT DONNE une machine à café
        var monnaie = new MonnaieTest();
        var brewer = new BrewerSpy();

        _ = new SoftwareMachineBuilder()
            .AyantUneMonnaie(monnaie)
            .AyantUnBrewer(brewer)
            .Build();

        // ALORS aucune invocation du Brewer ou de la ChangeMachine n'est effectuée
        Assert.True(monnaie.Untouched);
        Assert.True(brewer.Untouched);
    }

    [Fact]
    public void CasNominal()
    {
        // ETANT DONNE une machine à café
        var monnaie = new MonnaieTest();
        var brewer = new BrewerSpy(new BrewerStub());

        _ = new SoftwareMachineBuilder()
            .AyantUneMonnaie(monnaie)
            .AyantUnBrewer(brewer)
            .Build();

        // QUAND on insère une somme supérieure ou égale au prix d'un café
        monnaie.InsérerPièce(CoinCode.FiftyCents);

        // ALORS un café est préparé, la monnaie est encaissée et aucun remboursement n'a lieu
        BrewerAssert.CaféPréparé(brewer);
        ChangeMachineAssert.MonnaieEncaissée(monnaie);
        ChangeMachineAssert.AucunRemboursement(monnaie);
    }

    [Fact]
    public void CasBrewerDéfaillant()
    {
        // ETANT DONNE une machine à café ayant un brewer défaillant
        var monnaie = new MonnaieTest();

        _ = new SoftwareMachineBuilder()
            .AyantUnBrewer(new BrewerDummy())
            .AyantUneMonnaie(monnaie)
            .Build();

        // QUAND on insère une somme supérieure ou égale au prix d'un café
        monnaie.InsérerPièce(CoinCode.FiftyCents);

        // ALORS la monnaie est rendue et aucun encaissement n'a lieu
        ChangeMachineAssert.MonnaieRendue(monnaie);
        ChangeMachineAssert.AucunEncaissement(monnaie);
    }

    [Fact]
    public void PasAssezArgent()
    {
        // ETANT DONNE une machine à café
        var monnaie = new MonnaieTest();
        var brewer = new BrewerSpy();

        _ = new SoftwareMachineBuilder()
            .AyantUneMonnaie(monnaie)
            .AyantUnBrewer(brewer)
            .Build();

        // QUAND on insère moins que le prix d'un café
        monnaie.InsérerPièce(CoinCode.TwentyCents);

        // ALORS aucun café n'est préparé, la monnaie est rendue et aucun encaissement n'a lieu
        BrewerAssert.AucunCafé(brewer);
        ChangeMachineAssert.MonnaieRendue(monnaie);
        ChangeMachineAssert.AucunEncaissement(monnaie);
    }
}
