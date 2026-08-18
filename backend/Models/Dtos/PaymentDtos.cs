namespace WicStock_.Models.Dtos
{
    public class PaymentRequestDto
    {
        public int CommandeId { get; set; }
        public decimal Montant { get; set; }
        public string Currency { get; set; } = "TND";
        public string? AdresseLivraison { get; set; }
        public string? CodePostal { get; set; }
        public string? Ville { get; set; }
        public string? Pays { get; set; }
    }

    public class PaymentResponseDto
    {
        public string? CheckoutUrl { get; set; }
        public string? CheckoutId { get; set; }
        public decimal Montant { get; set; }
        public string? Currency { get; set; }
    }

    public class PaymentSuccessDto
    {
        public string? PaymentIntentId { get; set; }
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    public class MettreAJourAdresseDto
    {
        public string? AdresseLivraison { get; set; }
        public string? CodePostal { get; set; }
        public string? Ville { get; set; }
        public string? Pays { get; set; }
    }
}