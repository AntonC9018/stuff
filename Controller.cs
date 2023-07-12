using Microsoft.AspNetCore.Mvc;

namespace project;

public sealed class Model
{
    public IEnumerable<int> Ids { get; set; }
}

[ApiController]
[Route("test")]
public sealed class AController : Controller
{
    public AController()
    {
    }

    [HttpGet("model")]
    public ActionResult<Model> GetWithModel(Model model) => Ok(model);

    [HttpGet("model-query")]
    public ActionResult<Model> GetWithModelFromQuery([FromQuery] Model model) => Ok(model);
    
    [HttpGet("model-flat")]
    public ActionResult<Model> GetWithModelFromQueryFlat([FromQuery(Name = "")] Model model) => Ok(model);

    [HttpGet("no-model")]
    public ActionResult<IEnumerable<int>> GetWithoutModel(IEnumerable<int> model) => Ok(model);
    
    [HttpGet("no-model-query")]
    public ActionResult<IEnumerable<int>> GetWithoutModelFromQuery([FromQuery] IEnumerable<int> model) => Ok(model);
}