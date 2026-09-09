using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;
using ProhvatApp.Application.Rides.DTOs;

namespace ProhvatApp.Application.Common.Helpers;

public static class GpxHelper
{
    public static (List<GpxPointDto> Points, double TotalDistanceKm) ParseGpx(string gpxContent)
    {
        var points = new List<GpxPointDto>();
        if (string.IsNullOrWhiteSpace(gpxContent))
        {
            return (points, 0);
        }

        try
        {
            var doc = XDocument.Parse(gpxContent);
            XNamespace ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

            var trkpts = doc.Descendants(ns + "trkpt");
            if (!trkpts.Any())
            {
                // Fallback to searching without namespace or wpt
                trkpts = doc.Descendants().Where(e => e.Name.LocalName == "trkpt" || e.Name.LocalName == "wpt");
            }

            foreach (var pt in trkpts)
            {
                var latAttr = pt.Attribute("lat")?.Value;
                var lonAttr = pt.Attribute("lon")?.Value;

                if (double.TryParse(latAttr, NumberStyles.Any, CultureInfo.InvariantCulture, out var lat) &&
                    double.TryParse(lonAttr, NumberStyles.Any, CultureInfo.InvariantCulture, out var lon))
                {
                    double? ele = null;
                    var eleElem = pt.Elements().FirstOrDefault(e => e.Name.LocalName == "ele");
                    if (eleElem != null && double.TryParse(eleElem.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedEle))
                    {
                        ele = parsedEle;
                    }

                    points.Add(new GpxPointDto
                    {
                        Lat = lat,
                        Lng = lon,
                        Ele = ele
                    });
                }
            }

            double totalKm = 0;
            for (int i = 1; i < points.Count; i++)
            {
                totalKm += CalculateDistanceKm(points[i - 1].Lat, points[i - 1].Lng, points[i].Lat, points[i].Lng);
            }

            return (points, Math.Round(totalKm, 2));
        }
        catch
        {
            return (points, 0);
        }
    }

    private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        var r = 6371; // Earth radius in km
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return r * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
