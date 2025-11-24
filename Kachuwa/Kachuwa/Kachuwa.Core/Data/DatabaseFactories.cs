using System;
using System.Data;
using Kachuwa.Data.Crud;
using Kachuwa.Log;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kachuwa.Data
{
    /// <summary>
    /// This class is a factory class that creates 
    /// data-base specific factories which in turn create data acces objects
    /// </summary>
    public class DatabaseFactories
    {
        /// <summary>
        ///  gets a provider specific (i.e. database specific) factory 
        /// </summary>
        /// <param name="dialect"></param>
        /// <param name="serviceProvider"></param>
        /// <returns>an instance of service factory of given provider.</returns>
        public static IDatabaseFactory GetFactory()
        {
            return DbFactoryProvider.GetFactory();
        }

        public static IDatabaseFactory SetFactory(Dialect dialect, IServiceProvider serviceProvider)
        {
            // return the requested DaoFactory
            var configuration = serviceProvider.GetService<IConfiguration>();
            IDatabaseFactory dbFactory;
            switch (dialect)
            {
                case Dialect.SQLServer:
                    dbFactory = new MsSQLFactory(configuration, serviceProvider);
                    break;
                case Dialect.PostgreSQL:
                    dbFactory = new NpgSqlFactory(configuration, serviceProvider);
                    break;
                default:
                    dbFactory = new MsSQLFactory(configuration, serviceProvider);
                    break;
            }
            DbFactoryProvider.SetCurrentDbFactory(dbFactory);
            return dbFactory;
        }
    }
}