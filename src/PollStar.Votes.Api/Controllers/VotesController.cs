using Microsoft.AspNetCore.Mvc;
using PollStar.Votes.Abstractions.DataTransferObjects;
using PollStar.Votes.Abstractions.Services;

namespace PollStar.Votes.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VotesController : ControllerBase
    {
        private readonly IPollStarVotesService _service;

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            try
            {
                var dto = await _service.GetVotesAsync(id);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> Post(CastVoteDto dto)
        {
            try
            {
                 await _service.CastVoteAsync(dto);
                 return Accepted();
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        public VotesController(IPollStarVotesService service)
        {
            _service = service;
        }

    }
}
