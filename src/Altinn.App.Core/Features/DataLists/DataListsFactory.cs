﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.App.Core.Features.DataLists
{
    /// <summary>
    /// Factory class for resolving <see cref="IDataListProvider"/> implementations
    /// based on the name/id of the data lists requested.
    /// </summary>
    public class DataListsFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataListsFactory"/> class.
        /// </summary>
        public DataListsFactory(IEnumerable<IDataListProvider> dataListProviders)
        {
            DataListProviders = dataListProviders;
        }
        private IEnumerable<IDataListProvider> DataListProviders { get; }

        /// <summary>
        /// Finds the implementation of IDataListsProvider based on the options id
        /// provided.
        /// </summary>
        /// <param name="listId">Id matching the options requested.</param>
        public IDataListProvider GetDataListProvider(string listId)
        {
            foreach (var dataListProvider in DataListProviders)
            {
                if (dataListProvider.Id.ToLower().Equals(listId.ToLower()))
                {
                    return dataListProvider;
                }
            }

            return new NullDataListProvider();
        }
    }
}
