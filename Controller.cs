using Microsoft.AspNetCore.Mvc;

namespace project;

public sealed class Model
{
    public IEnumerable<int> Ids { get; set; }
}

[Route("test")]
public sealed class AController : Controller
{
    public AController()
    {
    }

    [HttpGet("model")]
    public IActionResult GetWithModel(Model model) => Ok(model);

    [HttpGet("model-query")]
    public IActionResult GetWithModelFromQuery([FromQuery] Model model) => Ok(model);
    
    [HttpGet("model-flat")]
    public IActionResult GetWithModelFromQueryFlat([FromQuery(Name = "")] Model model) => Ok(model);

    [HttpGet("no-model")]
    public IActionResult GetWithoutModel(IEnumerable<int> model) => Ok(model);
}