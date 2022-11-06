using Boost.Proto.Actor.DependencyInjection;
using Boost.Proto.Actor.Hosting.Cluster;
using Microsoft.Extensions.Options;
using Ports.Smtp;
using Ports.Smtp.Actors;
using Proto.Router;
using SendMailService;
using SendMailService.Actors;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseProtoActorCluster((option, sp) =>
{
    option.Provider = ClusterProviderType.Local;
    option.Name = "test";

    option.ClusterKinds.Add(new
    (
        nameof(EmailSagaGrain),
        sp.GetRequiredService<IPropsFactory<EmailSagaGrain>>().Create()
    ));

    option.FuncActorSystemStart = root =>
    {
        var pid = root.SpawnNamed(root.NewRoundRobinPool(sp.GetRequiredService<IPropsFactory<SmtpPortActor>>().Create(), 10), nameof(SmtpPortActor));

        return root;
    };
});
builder.Host.UseSmtp((option, sp) =>
{
    option.Smtp = option.Smtp with
    {
        Host = "127.0.0.1"
    };
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

var option = app.Services.GetRequiredService<IOptions<SmtpOptions>>().Value;

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
