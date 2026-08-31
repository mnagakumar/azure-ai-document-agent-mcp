namespace DocumentAgent.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using DocumentAgent.Mcp;

[ApiController]
[Route("api/mcp")]
public class McpController : ControllerBase
{
    private readonly IMcpServer _mcpServer;
    private readonly ILogger<McpController> _logger;

    public McpController(IMcpServer mcpServer, ILogger<McpController> logger)
    {
        _mcpServer = mcpServer;
        _logger = logger;
    }

    /// <summary>
    /// Process MCP requests (Model Context Protocol)
    /// </summary>
    [HttpPost("invoke")]
    [ProducesResponseType(typeof(McpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InvokeMcp([FromBody] McpRequest request)
    {
        try
        {
            if (request == null)
                return BadRequest(new { error = "Invalid MCP request" });

            _logger.LogInformation($"Processing MCP request: {request.Method}");
            var response = await _mcpServer.ProcessRequestAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MCP request");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get available MCP tools
    /// </summary>
    [HttpGet("tools")]
    [ProducesResponseType(typeof(List<McpTool>), StatusCodes.Status200OK)]
    public IActionResult GetTools()
    {
        try
        {
            _logger.LogInformation("Getting available MCP tools");
            var tools = _mcpServer.GetAvailableTools();
            return Ok(tools);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting MCP tools");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get available MCP resources
    /// </summary>
    [HttpGet("resources")]
    [ProducesResponseType(typeof(List<McpResource>), StatusCodes.Status200OK)]
    public IActionResult GetResources()
    {
        try
        {
            _logger.LogInformation("Getting available MCP resources");
            var resources = _mcpServer.GetAvailableResources();
            return Ok(resources);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting MCP resources");
            return BadRequest(new { error = ex.Message });
        }
    }
}
