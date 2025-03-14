using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinWallet.Repository.Payload.Response.WalletResponse
{
    public class WalletContributionResponse
    {
        public decimal TotalContribution { get; set; }
        public string TimeFrame { get; set; }
        public List<MemberContributionDto> Members { get; set; }
    }

    public class MemberContributionDto
    {
        public string Name { get; set; }
        public decimal Contribution { get; set; }
        public decimal Percentage { get; set; }
    }
}
