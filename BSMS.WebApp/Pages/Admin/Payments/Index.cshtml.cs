using BSMS.BLL.Services;
using BSMS.BusinessObjects.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSMS.WebApp.Pages.Admin.Payments
{
    public class IndexModel : PageModel
    {
        private readonly IPaymentService _service;
        [BindProperty(SupportsGet = true)] public int? UserId { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Start { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? End { get; set; }
        [BindProperty(SupportsGet = true)] public string? Status { get; set; }

        public List<Payment> Payments { get; set; } = new();

        public IndexModel(IPaymentService service) => _service = service;

        public async Task OnGetAsync()
        {
            Payments = await _service.GetFilteredAsync(UserId, Start, End, Status);
        }
    }
}
