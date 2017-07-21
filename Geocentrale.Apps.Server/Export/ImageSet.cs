using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Geocentrale.Apps.DataContracts;
using System.Drawing;

namespace Geocentrale.Apps.Server.Export
{
    public enum GeoDatasetType
    {
        Baselayer,
        Topiclayer,
        Additionallayer,
        AdditionallayerBackground
    }

    public class ImageSet
    {
        public Image Image { get; set; }
        public IGAGeoDataSet GeoDataset { get; set; }
        public GeoDatasetType Type { get; set; }
        public GAExtent Extent { get; set; }
        public GASize Size { get; set; }
        public string MimeType { get; set; } = "image/png";

        //All items has the same extent and size
        public string ImageKey => $"{GeoDataset.Guid}"; //$"{imageItem.GeoDataset.Guid};{imageItem.Extent.Xmin};{imageItem.Extent.Ymin};{imageItem.Extent.Xmax};{imageItem.Extent.Ymax};{imageItem.Size.Width};{imageItem.Size.Height}"

    }
}