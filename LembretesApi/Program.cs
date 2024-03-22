using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Adiciona serviços necessários para suportar controllers na aplicação
builder.Services.AddControllers();

var app = builder.Build();

// Configura o pipeline de requisições HTTP
if (app.Environment.IsDevelopment())
{
    // Adiciona uma página de erro detalhada para ambiente de desenvolvimento
    app.UseDeveloperExceptionPage();
}
else
{
    // Redireciona para a página de erro em caso de exceção
    app.UseExceptionHandler("/Error");
    // Habilita o HTTP Strict Transport Security (HSTS) para melhorar a segurança
    app.UseHsts();
}

// Redireciona requisições HTTP para HTTPS
app.UseHttpsRedirection();

// Configura o uso de arquivos padrão (como index.html) para requisições para diretórios
app.UseDefaultFiles(new DefaultFilesOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "../public")),
    RequestPath = new Microsoft.AspNetCore.Http.PathString("")
});

// Configura o uso de arquivos estáticos (CSS, JavaScript, etc.)
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "../public")),
    RequestPath = ""
});

// Habilita o roteamento de requisições
app.UseRouting();

// Habilita a autorização de requisições
app.UseAuthorization();

// Mapeia os controllers para que sejam acessíveis pelas requisições HTTP
app.MapControllers();

// Executa a aplicação web
app.Run();
