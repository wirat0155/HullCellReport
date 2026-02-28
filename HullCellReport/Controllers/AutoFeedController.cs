using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace HullCellReport.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AutoFeedController : ControllerBase
    {
        public class AutoFeedData
        {
            public string autofeedname { get; set; }
            public float autofeelml { get; set; }
        }

        [HttpPost("ReceiveData")]
        public IActionResult ReceiveData([FromBody] List<AutoFeedData> data)
        {
            // Debug: Place a breakpoint here to check if the 'data' is received correctly
            return Ok(new { success = true, received_count = data?.Count ?? 0, data = data });
        }
    }
}
