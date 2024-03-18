using System.ComponentModel.DataAnnotations;

namespace Altinn.App.Core.Features.Payment.Providers.Nets.Models
{
    public class NetsCancelPayment
    {
        [Required]
        /// <summary>
        /// The amount to be canceled.
        /// Must be higher than 0.
        /// </summary>
        public int Amount { get; set; }

        /// <summary>
        /// The order items to be canceled. Note! Since only full cancels are currently supported, you need to provide all order items or completely avoid specifying any order items.
        /// </summary>
        public NetsOrderItem[]? OrderItems { get; set; }
    }
}