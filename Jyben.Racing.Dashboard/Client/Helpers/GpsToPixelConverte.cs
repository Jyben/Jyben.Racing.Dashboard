using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet;
using System.Drawing;

namespace Jyben.Racing.Dashboard.Client.Helpers
{
    public class GpsToPixelConverter
    {
        private double _canvasWidth; // Largeur du canevas en pixels
        private double _canvasHeight; // Hauteur du canevas en pixels
        private double _leftLongitude; // Longitude du coin supérieur gauche du canevas
        private double _topLatitude; // Latitude du coin supérieur gauche du canevas
        private double _rightLongitude; // Longitude du coin inférieur droit du canevas
        private double _bottomLatitude; // Latitude du coin inférieur droit du canevas
        private double _pixelsPerDegreeLongitude; // Pixels par degré de longitude
        private double _pixelsPerDegreeLatitude; // Pixels par degré de latitude

        private ICoordinateTransformation _coordTransform;

        public GpsToPixelConverter(double canvasWidth, double canvasHeight,
            double leftLongitude, double topLatitude,
            double rightLongitude, double bottomLatitude)
        {
            _canvasWidth = canvasWidth;
            _canvasHeight = canvasHeight;
            _leftLongitude = leftLongitude;
            _topLatitude = topLatitude;
            _rightLongitude = rightLongitude;
            _bottomLatitude = bottomLatitude;

            // Calculer les pixels par degré de longitude et de latitude
            _pixelsPerDegreeLongitude = canvasWidth / (rightLongitude - leftLongitude);
            _pixelsPerDegreeLatitude = canvasHeight / (bottomLatitude - topLatitude);

            _pixelsPerDegreeLongitude = canvasWidth / (_rightLongitude - _leftLongitude);
            _pixelsPerDegreeLatitude = canvasHeight / (_topLatitude - _bottomLatitude);
        }

        public Point Convert(double longitude, double latitude)
        {
            double pixelX = _pixelsPerDegreeLongitude * (longitude - _leftLongitude);
            double pixelY = _pixelsPerDegreeLatitude * (_topLatitude - latitude);

            return new Point((int)Math.Round(pixelX), (int)Math.Round(pixelY));
        }
    }
}

