using CommonDevPack.Infra.Cache.Redis.Interfaces;
using Microsoft.AspNetCore.Mvc;
using PocSessionRedis.Api.Model;

namespace PocSessionRedis.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly ILogger<UserController> _logger;
    private readonly IRedisService _redisService;

    public UserController(ILogger<UserController> logger, IRedisService redisService)
    {
        _logger = logger;
        _redisService = redisService;
    }

    /// <summary>
    /// Obter todos usuário cadastrados
    /// </summary>
    /// <returns></returns>
    [HttpGet()]
    public async Task<IActionResult> GetAll()
    {
        var users = await _redisService.GetAsync<List<User>>("users");
        return Ok(users);
    }

    /// <summary>
    /// Carregar usuários para banco de dados no redis
    /// </summary>
    /// <returns></returns>
    [HttpGet("load")]
    public async Task<IActionResult> LoadUsers()
    {
        //obter todas as keys no redis, baseado no prefixo "session_"
        var keys = _redisService.GetAllKeysAsync("session_");
        await foreach (var item in keys)
        {
            //deleta todas as chaves encontradas
            await _redisService.DeleteAsync(item);
        }

        //deleta todos os usuários do banco de dados fake
        await _redisService.DeleteAsync("users");

        //gera usuários fakes
        var users = Model.User.GenerateUsers();
        //gravas usuários no banco de dados fake
        await _redisService.SetAsync("users", users);

        return Ok("Database carregado.");
    }

    /// <summary>
    /// Realizar logon
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    [HttpPost("logon")]
    public async Task<IActionResult> Logon([FromBody] UserLogon user)
    {
        //simples checagem para verificar se username e password foram informados
        if (string.IsNullOrWhiteSpace(user.UserName) ||
           string.IsNullOrWhiteSpace(user.Password))
        {
            return BadRequest("Usuário/senha não informado.");
        }

        //tenta obter o usuário no redis a partir da key "session_[USERNAME]"
        var result = await _redisService.GetAsync<User>($"session_{user.UserName.ToLower()}");
        if (result != null)
        {
            //caso encontre, sinaliza que já possui uma sessão ativa
            return BadRequest(new
            {
                message = "Usuário já possui uma sessão ativa."
            });
        }
        
        //<<SUA LÓGICA PARA AUTENTICAR O USUÁRIO, VALIDANDO SEU USUÁRIO E SENHA, POR EXEMPLO>>

        //caso não exista, cadastrar a sessão do usuário no redis
        await _redisService.SetAsync($"session_{user.UserName.ToLower()}", 
            new User
            {
                UserName = user.UserName,
                LoggedAt = DateTime.Now
            });
        
        return Ok(new
        {
            message = "Usuário logado com sucesso."
        });
    }

    /// <summary>
    /// Realizar logout
    /// </summary>
    /// <param name="userName"></param>
    /// <returns></returns>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(string userName)
    {
        //tenta obter o usuário no redis a partir do prefixo "session_"
        var result = await _redisService.GetAsync<User>($"session_{userName.ToLower()}");
        if (result == null)
        {
            //caso não encontre, sinaliza a impossibilidade de realizar logout
            return BadRequest(new
            {
                message = "Usuário não encontrado para realizar logout."
            });
        }

        //caso encontre, remove a sessão do redis
        await _redisService.DeleteAsync($"session_{userName.ToLower()}");

        return Ok(new
        {
            message = "Logout."
        });
    }

    
}
