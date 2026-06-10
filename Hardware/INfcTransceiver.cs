namespace Hardware;

public interface INfcTransceiver
{
    /// <summary>
    /// Evènement déclenché lorsqu'un appareil apparaît ou disparaît à proximité du capteur NFC.
    /// </summary>
    event Action<NfcState> NfcStateChanged;

    /// <summary>
    /// Tente de débiter une somme depuis la clé café.
    /// </summary>
    /// <param name="amountInCents">Montant en centimes à débiter.</param>
    /// <returns>
    /// false si aucune clé n'est présente.
    /// false si la clé ne contient pas assez d'argent.
    /// false en cas d'erreur.
    /// true si l'argent a été débité.
    /// </returns>
    bool TryChargeAmount(ushort amountInCents);

    /// <summary>
    /// Tente de recharger la clé café
    /// </summary>
    /// <param name="amountInCents">Le montant à recharger.</param>
    /// <returns>
    /// false si aucune clé n'est présente.
    /// false si la clé n'est pas rechargeable.
    /// false si le plafond est atteint.
    /// false en cas d'erreur.
    /// true si la recharge est effective.
    /// </returns>
    bool TryRefillDevice(ushort amountInCents);
}