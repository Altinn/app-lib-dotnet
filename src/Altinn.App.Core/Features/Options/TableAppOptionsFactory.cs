using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.App.Core.Features.Options
{
    public class TableAppOptionsFactory
    {
        public TableAppOptionsFactory(IEnumerable<ITableAppOptionsProvider> tableAppOptionsProviders)
        {
            _tableAppOptionsProviders = tableAppOptionsProviders;
        }
        private IEnumerable<ITableAppOptionsProvider> _tableAppOptionsProviders { get; }

        /// <summary>
        /// Finds the implementation of ITableAppOptionsProvider based on the options id
        /// provided.
        /// </summary>
        /// <param name="optionsId">Id matching the options requested.</param>
        public ITableAppOptionsProvider GetOptionsProvider(string optionsId)
        {
            foreach (var tableAppOptionProvider in _tableAppOptionsProviders)
            {
                if (tableAppOptionProvider.Id.ToLower() != optionsId.ToLower())
                {
                    continue;
                }

                return tableAppOptionProvider;
            }

            return new NullTableAppOptionsProvider();

        }
    }
}
