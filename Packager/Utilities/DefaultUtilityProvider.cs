﻿using Packager.Models;
using Packager.Providers;

namespace Packager.Utilities
{
    public class DefaultUtilityProvider : IUtilityProvider
    {
        public DefaultUtilityProvider()
        {
            CarrierDataExcelImporter = new ExcelImporter<CarrierData>();
            BextDataProvider = new StandInBextDataProvider();
            Hasher = new Hasher();
            UserInfoResolver = new DomainUserResolver();
            XmlExporter = new XmlExporter();
            DirectoryProvider = new DirectoryProvider();
        }

        public IExcelImporter CarrierDataExcelImporter { get; private set; }
        public IBextDataProvider BextDataProvider { get; private set; }
        public IHasher Hasher { get; private set; }
        public IUserInfoResolver UserInfoResolver { get; private set; }
        public IXmlExporter XmlExporter { get; private set; }
        public IDirectoryProvider DirectoryProvider { get; private set; }
    }
}