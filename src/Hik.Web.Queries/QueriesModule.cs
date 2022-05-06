using Autofac;
using Autofac.Core;
using Hik.DataAccess;
using Hik.DataAccess.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Hik.Web.Queries
{
    public class QueriesModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .Register<IDbConfiguration>(context =>
                context.Resolve<IOptionsSnapshot<Hik.DataAccess.DbConfiguration>>().Value);

            builder
                .RegisterType<DataContext>()
                .As<DbContext>()
                .As<DataContext>();

            builder
                .RegisterGeneric(typeof(BaseRepository<>))
                .WithParameter(new ResolvedParameter(
                    (pi, _) => pi.ParameterType == typeof(DbContext),
                    (_, context) => context.Resolve<DataContext>()))
                .As(typeof(IBaseRepository<>));

            builder.RegisterType<HikDatabase>()
                .As<IHikDatabase>();
            builder.RegisterType<UnitOfWorkFactory>()
                .As<IUnitOfWorkFactory>();

            System.Reflection.Assembly queriesAssembly = typeof(QueriesModule).Assembly;

            builder
                .RegisterAssemblyTypes(queriesAssembly)
                .AsClosedTypesOf(typeof(IRequestHandler<>))
                .AsImplementedInterfaces();

            builder
                .RegisterAssemblyTypes(queriesAssembly)
                .AsClosedTypesOf(typeof(IRequestHandler<,>))
                .AsImplementedInterfaces();
        }
    }
}