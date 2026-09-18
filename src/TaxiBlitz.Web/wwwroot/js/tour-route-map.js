window.TourRouteMap = (function () {
    function init(mapId, routePoints) {
        var container = document.getElementById(mapId);
        if (!container || !window.L) {
            return;
        }

        var validPoints = (routePoints || []).filter(function (point) {
            return point && typeof point.latitude === 'number' && typeof point.longitude === 'number';
        });

        var map = L.map(mapId, { scrollWheelZoom: false });
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '&copy; OpenStreetMap contributors'
        }).addTo(map);

        if (!validPoints.length) {
            map.setView([41.117, 20.801], 9);
            L.popup()
                .setLatLng([41.117, 20.801])
                .setContent('Route map will appear once the itinerary locations are available.')
                .openOn(map);
            return;
        }

        var bounds = [];
        var polylineLatLngs = [];

        validPoints.forEach(function (point) {
            var latLng = [point.latitude, point.longitude];
            polylineLatLngs.push(latLng);
            bounds.push(latLng);

            var marker = L.marker(latLng).addTo(map);
            var popupHtml = '<div style="min-width:180px;">' +
                '<strong>' + escapeHtml(point.name || 'Stop') + '</strong>';

            if (point.description) {
                popupHtml += '<div style="margin-top:4px;color:#64748b;font-size:0.88rem;">' + escapeHtml(point.description) + '</div>';
            }

            popupHtml += '</div>';
            marker.bindPopup(popupHtml);
        });

        if (polylineLatLngs.length > 1) {
            L.polyline(polylineLatLngs, {
                color: '#0f766e',
                weight: 5,
                opacity: 0.85,
                lineJoin: 'round'
            }).addTo(map);
        }

        if (bounds.length === 1) {
            map.setView(bounds[0], 13);
        } else {
            map.fitBounds(bounds, { padding: [30, 30] });
        }
    }

    function escapeHtml(value) {
        return String(value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    return {
        init: init
    };
})();
