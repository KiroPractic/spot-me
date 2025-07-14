// Professional World Map using Leaflet.js with real country boundaries
window.worldMapInstances = {};

// Mapping from country names to ISO codes
const countryNameToIsoCode = {
    'Afghanistan': 'af',
    'Albania': 'al',
    'Algeria': 'dz',
    'Angola': 'ao',
    'Argentina': 'ar',
    'Armenia': 'am',
    'Australia': 'au',
    'Austria': 'at',
    'Azerbaijan': 'az',
    'Bahrain': 'bh',
    'Bangladesh': 'bd',
    'Belarus': 'by',
    'Belgium': 'be',
    'Benin': 'bj',
    'Bolivia': 'bo',
    'Bosnia and Herzegovina': 'ba',
    'Botswana': 'bw',
    'Brazil': 'br',
    'Brunei': 'bn',
    'Bulgaria': 'bg',
    'Burkina Faso': 'bf',
    'Cambodia': 'kh',
    'Cameroon': 'cm',
    'Canada': 'ca',
    'Central African Republic': 'cf',
    'Chad': 'td',
    'Chile': 'cl',
    'China': 'cn',
    'Colombia': 'co',
    'Costa Rica': 'cr',
    'Croatia': 'hr',
    'Cuba': 'cu',
    'Cyprus': 'cy',
    'Czech Republic': 'cz',
    'Czechia': 'cz',
    'Democratic Republic of the Congo': 'cd',
    'Denmark': 'dk',
    'Dominican Republic': 'do',
    'Ecuador': 'ec',
    'Egypt': 'eg',
    'El Salvador': 'sv',
    'Estonia': 'ee',
    'Ethiopia': 'et',
    'Finland': 'fi',
    'France': 'fr',
    'Gabon': 'ga',
    'Georgia': 'ge',
    'Germany': 'de',
    'Ghana': 'gh',
    'Greece': 'gr',
    'Guatemala': 'gt',
    'Guinea': 'gn',
    'Honduras': 'hn',
    'Hungary': 'hu',
    'Iceland': 'is',
    'India': 'in',
    'Indonesia': 'id',
    'Iran': 'ir',
    'Iraq': 'iq',
    'Ireland': 'ie',
    'Israel': 'il',
    'Italy': 'it',
    'Japan': 'jp',
    'Jordan': 'jo',
    'Kazakhstan': 'kz',
    'Kenya': 'ke',
    'Kuwait': 'kw',
    'Latvia': 'lv',
    'Lebanon': 'lb',
    'Libya': 'ly',
    'Lithuania': 'lt',
    'Luxembourg': 'lu',
    'Madagascar': 'mg',
    'Malaysia': 'my',
    'Mali': 'ml',
    'Mexico': 'mx',
    'Moldova': 'md',
    'Mongolia': 'mn',
    'Morocco': 'ma',
    'Myanmar': 'mm',
    'Nepal': 'np',
    'Netherlands': 'nl',
    'New Zealand': 'nz',
    'Nicaragua': 'ni',
    'Niger': 'ne',
    'Nigeria': 'ng',
    'North Korea': 'kp',
    'Norway': 'no',
    'Oman': 'om',
    'Pakistan': 'pk',
    'Panama': 'pa',
    'Paraguay': 'py',
    'Peru': 'pe',
    'Philippines': 'ph',
    'Poland': 'pl',
    'Portugal': 'pt',
    'Qatar': 'qa',
    'Romania': 'ro',
    'Russia': 'ru',
    'Russian Federation': 'ru',
    'Saudi Arabia': 'sa',
    'Senegal': 'sn',
    'Serbia': 'rs',
    'Singapore': 'sg',
    'Slovakia': 'sk',
    'Slovenia': 'si',
    'South Africa': 'za',
    'South Korea': 'kr',
    'Spain': 'es',
    'Sri Lanka': 'lk',
    'Sudan': 'sd',
    'Sweden': 'se',
    'Switzerland': 'ch',
    'Syria': 'sy',
    'Taiwan': 'tw',
    'Thailand': 'th',
    'Tunisia': 'tn',
    'Turkey': 'tr',
    'Ukraine': 'ua',
    'United Arab Emirates': 'ae',
    'United Kingdom': 'gb',
    'United Kingdom of Great Britain and Northern Ireland': 'gb',
    'Great Britain': 'gb',
    'Britain': 'gb',
    'England': 'gb',
    'Scotland': 'gb',
    'Wales': 'gb',
    'Northern Ireland': 'gb',
    'United States of America': 'us',
    'United States': 'us',
    'Uruguay': 'uy',
    'Venezuela': 've',
    'Vietnam': 'vn',
    'Yemen': 'ye',
    'Zambia': 'zm',
    'Zimbabwe': 'zw'
};

// Initialize a professional world map with Leaflet
window.initializeWorldMap = function(canvasId, mapData) {
    try {
        console.log('Initializing professional world map for:', canvasId);

        // Get the container div (not canvas)
        const mapContainer = document.getElementById(canvasId);
        if (!mapContainer) {
            console.error('Map container not found:', canvasId);
            return;
        }

        // Check if Leaflet is available
        if (typeof L === 'undefined') {
            console.error('Leaflet.js is not loaded');
            showMapError(canvasId, 'Leaflet.js library is not loaded');
            return;
        }

        // Clear any existing content
        mapContainer.innerHTML = '';

        // Set up the container for Leaflet
        mapContainer.style.width = '100%';
        mapContainer.style.height = '100%';
        mapContainer.style.backgroundColor = '#121212';
        mapContainer.style.position = 'relative';

        // Initialize Leaflet map
        const map = L.map(mapContainer, {
            center: [20, 0],
            zoom: 2,
            minZoom: 1,
            maxZoom: 6,
            zoomControl: true,
            scrollWheelZoom: true,
            doubleClickZoom: true,
            boxZoom: true,
            keyboard: true,
            dragging: true,
            touchZoom: true,
            attributionControl: false,
            worldCopyJump: false, // Prevent world wrapping
            maxBounds: [[-90, -180], [90, 180]], // Limit to one world view
            maxBoundsViscosity: 1.0
        });

        // Add dark tile layer that matches your theme
        L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
            attribution: '',
            subdomains: 'abcd',
            maxZoom: 19
        }).addTo(map);

        // Remove default click outline/frame aggressively
        map.on('click', function(e) {
            // Remove focus from any element
            if (document.activeElement) {
                document.activeElement.blur();
            }

            // Reset all layer styles
            map.eachLayer(function(layer) {
                if (layer.setStyle && layer.feature) {
                    layer.setStyle({
                        weight: 1,
                        opacity: 1,
                        color: '#121212'
                    });
                }
            });
        });

        // Also handle any SVG element clicks
        map.getContainer().addEventListener('click', function(e) {
            if (e.target.tagName === 'path' || e.target.tagName === 'svg') {
                e.target.style.outline = 'none';
                e.target.style.stroke = '#121212';
                e.target.style.strokeWidth = '1px';
            }
        });

        // Store map instance
        window.worldMapInstances[canvasId] = {
            map: map,
            mapContainer: mapContainer,
            data: mapData,
            countryLayers: null
        };

        // Load and display country data
        loadCountryBoundaries(canvasId, mapData);

        console.log('Professional world map initialized successfully');

    } catch (error) {
        console.error('Error initializing world map:', error);
        showMapError(canvasId, 'Failed to load world map: ' + error.message);
    }
};



// Load country boundaries and apply data
function loadCountryBoundaries(canvasId, mapData) {
    const instance = window.worldMapInstances[canvasId];
    if (!instance) {
        console.error('No map instance found for:', canvasId);
        return;
    }

    console.log('Loading country boundaries for map:', canvasId);
    console.log('Map data:', mapData);

    // Use the original GeoJSON source but handle territories differently
    const geoJsonUrl = 'https://raw.githubusercontent.com/holtzy/D3-graph-gallery/master/DATA/world.geojson';

    fetch(geoJsonUrl)
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response.json();
        })
        .then(geoData => {
            console.log('Country boundaries loaded successfully, features:', geoData.features?.length);

            // Debug: Check what properties are available in the first feature
            if (geoData.features && geoData.features.length > 0) {
                console.log('First feature properties:', Object.keys(geoData.features[0].properties));
                console.log('First feature sample:', geoData.features[0].properties);
            }

            // Debug: Check what country codes are available in GeoJSON
            const geoCountryCodes = geoData.features.map(f => f.properties.ISO_A2 || f.properties.iso_a2 || f.properties.ISO2 || f.properties.iso2).filter(code => code && code !== '-99');
            console.log('Available GeoJSON country codes (first 20):', geoCountryCodes.slice(0, 20));

            // Calculate color scale with better distribution
            const values = Object.values(mapData.data || {}).map(d => d.value);
            const minValue = Math.min(...values) || 0;
            const maxValue = Math.max(...values) || 1;

            // Use logarithmic scaling to handle extreme outliers like Croatia
            const logValues = values.map(v => Math.log10(v + 1));
            const logMin = Math.min(...logValues);
            const logMax = Math.max(...logValues);

            console.log('Data range:', minValue, 'to', maxValue);
            console.log('Log range:', logMin, 'to', logMax);
            console.log('Countries with data:', Object.keys(mapData.data || {}));

            // Debug: Check for matches
            const dataCountryCodes = Object.keys(mapData.data || {}).map(code => code.toUpperCase());
            const matches = dataCountryCodes.filter(code => geoCountryCodes.includes(code));
            console.log('Matching country codes:', matches);
            console.log('Non-matching data codes:', dataCountryCodes.filter(code => !geoCountryCodes.includes(code)));

            // Add countries to map
            let countriesWithData = 0;
            let debugCount = 0;
            const countryLayer = L.geoJSON(geoData, {
                style: function(feature) {
                    // Get country name from GeoJSON
                    const countryName = feature.properties.name || 'Unknown';

                    // Check if this is an overseas territory
                    const isOverseasTerritory = [
                        'French Guiana', 'Martinique', 'Guadeloupe', 'Réunion', 'Mayotte',
                        'New Caledonia', 'French Polynesia', 'Saint Pierre and Miquelon',
                        'Puerto Rico', 'U.S. Virgin Islands', 'American Samoa', 'Guam',
                        'Northern Mariana Islands', 'Greenland', 'Faroe Islands'
                    ].includes(countryName);

                    // For overseas territories, don't apply data styling
                    if (isOverseasTerritory) {
                        return {
                            fillColor: '#2a2a2a', // Darker gray for territories
                            weight: 1,
                            opacity: 1,
                            color: '#121212',
                            fillOpacity: 0.3
                        };
                    }

                    // Map country name to ISO code for main countries
                    const countryCode = countryNameToIsoCode[countryName]?.toLowerCase();

                    // Look up data using the mapped ISO code
                    const countryData = countryCode ? mapData.data?.[countryCode] : null;

                    // Debug: Log first few countries being processed
                    if (debugCount < 10) {
                        console.log(`Processing country: ${countryName} -> ${countryCode}`);
                        console.log(`Has data:`, !!countryData);
                        if (countryData) {
                            console.log(`Data:`, countryData);
                            countriesWithData++;
                        }
                        debugCount++;
                    }

                    // Special debug for UK
                    if (countryName.toLowerCase().includes('kingdom') || countryName.toLowerCase().includes('britain') || countryName.toLowerCase().includes('england')) {
                        console.log(`UK DEBUG: Found country "${countryName}" -> mapped to "${countryCode}" -> has data: ${!!countryData}`);
                        if (mapData.data && mapData.data.gb) {
                            console.log(`UK data available:`, mapData.data.gb);
                        }
                    }

                    if (countryData) {
                        // Use logarithmic scaling for better distribution
                        const logValue = Math.log10(countryData.value + 1);
                        const intensity = logMax > logMin ? (logValue - logMin) / (logMax - logMin) : 0.5;

                        // Use more vibrant colors: darker blue to bright green
                        const color = interpolateColor('#1e3a8a', '#1DB954', intensity); // Dark blue to Spotify green

                        if (debugCount < 5) {
                            console.log(`Styling ${countryName}: value=${countryData.value}, logValue=${logValue.toFixed(2)}, intensity=${intensity.toFixed(2)}, color=${color}`);
                        }

                        return {
                            fillColor: color,
                            weight: 1,
                            opacity: 1,
                            color: '#121212',
                            fillOpacity: 0.9
                        };
                    } else {
                        return {
                            fillColor: '#404040', // More visible gray
                            weight: 1,
                            opacity: 1,
                            color: '#121212',
                            fillOpacity: 0.4
                        };
                    }
                },
                onEachFeature: function(feature, layer) {
                    // Use the same logic as in the style function
                    const countryName = feature.properties.name || 'Unknown';

                    // Check if this is an overseas territory
                    const isOverseasTerritory = [
                        'French Guiana', 'Martinique', 'Guadeloupe', 'Réunion', 'Mayotte',
                        'New Caledonia', 'French Polynesia', 'Saint Pierre and Miquelon',
                        'Puerto Rico', 'U.S. Virgin Islands', 'American Samoa', 'Guam',
                        'Northern Mariana Islands', 'Greenland', 'Faroe Islands'
                    ].includes(countryName);

                    // Don't add tooltips to overseas territories
                    if (isOverseasTerritory) {
                        return;
                    }

                    const countryCode = countryNameToIsoCode[countryName]?.toLowerCase();
                    const countryData = countryCode ? mapData.data?.[countryCode] : null;

                    if (countryData) {
                        // Add tooltip with strict geographic filtering
                        layer.on('mouseover', function(e) {
                            const latlng = e.latlng;

                            // Define STRICT main country regions (only mainland)
                            const mainCountryBounds = {
                                'France': { north: 51.2, south: 42.3, west: -4.8, east: 8.2 },
                                'United States': { north: 49.0, south: 25.0, west: -125.0, east: -66.9 },
                                'United Kingdom': { north: 60.8, south: 49.9, west: -7.6, east: 1.8 },
                                'Spain': { north: 43.8, south: 36.0, west: -9.3, east: 3.3 },
                                'Portugal': { north: 42.2, south: 36.9, west: -9.5, east: -6.2 },
                                'Netherlands': { north: 53.6, south: 50.7, west: 3.2, east: 7.2 },
                                'Denmark': { north: 57.8, south: 54.5, west: 8.0, east: 15.2 }
                            };

                            const bounds = mainCountryBounds[countryName];
                            let showTooltip = false;

                            // Only show tooltip if within STRICT main country bounds
                            if (bounds) {
                                showTooltip = latlng.lat >= bounds.south &&
                                            latlng.lat <= bounds.north &&
                                            latlng.lng >= bounds.west &&
                                            latlng.lng <= bounds.east;
                            } else {
                                // For countries not in the bounds list, show tooltip everywhere
                                showTooltip = true;
                            }

                            if (showTooltip) {
                                layer.bindTooltip(`
                                    <div style="font-weight: 600; color: #1DB954; margin-bottom: 4px;">
                                        ${countryName}
                                    </div>
                                    <div style="color: #b3b3b3; font-size: 12px;">
                                        ${countryData.label || 'No data'}
                                    </div>
                                `, {
                                    permanent: false,
                                    direction: 'top',
                                    className: 'custom-tooltip',
                                    opacity: 1
                                }).openTooltip();
                            }
                        });

                        layer.on('mouseout', function() {
                            layer.closeTooltip();
                        });

                        // Add click event without outline
                        layer.on('click', function(e) {
                            // Completely prevent any selection styling
                            e.originalEvent.preventDefault();
                            e.originalEvent.stopPropagation();

                            // Force remove any selection styling
                            setTimeout(() => {
                                e.target.setStyle({
                                    weight: 1,
                                    opacity: 1,
                                    color: '#121212',
                                    fillOpacity: e.target.options.fillOpacity
                                });
                            }, 0);

                            console.log('Clicked country:', countryName, countryData);
                        });
                    }
                }
            }).addTo(instance.map);

            instance.countryLayers = countryLayer;
            console.log('Country layer added to map successfully');

        })
        .catch(error => {
            console.error('Error loading country boundaries:', error);
            console.log('Falling back to marker-based visualization');

            // Fallback: show countries as markers
            showCountriesAsMarkers(canvasId, mapData);
        });
}

// Update existing world map data
window.updateWorldMapData = function(canvasId, mapData) {
    try {
        const instance = window.worldMapInstances[canvasId];
        if (!instance) {
            console.log('Map instance not found, initializing new one');
            initializeWorldMap(canvasId, mapData);
            return;
        }

        // Update the stored data
        instance.data = mapData;

        // Remove existing country layers
        if (instance.countryLayers) {
            instance.map.removeLayer(instance.countryLayers);
        }

        // Reload with new data
        loadCountryBoundaries(canvasId, mapData);

        console.log('World map data updated successfully');

    } catch (error) {
        console.error('Error updating world map data:', error);
        // Fallback to reinitialize
        initializeWorldMap(canvasId, mapData);
    }
};

// Fallback: show countries as markers when GeoJSON fails
function showCountriesAsMarkers(canvasId, mapData) {
    const instance = window.worldMapInstances[canvasId];
    if (!instance || !mapData.data) return;

    console.log('Creating marker-based visualization');

    // Simple country coordinates (approximate centers)
    const countryCoords = {
        'us': [39.8283, -98.5795],
        'ca': [56.1304, -106.3468],
        'gb': [55.3781, -3.4360],
        'de': [51.1657, 10.4515],
        'fr': [46.2276, 2.2137],
        'it': [41.8719, 12.5674],
        'es': [40.4637, -3.7492],
        'nl': [52.1326, 5.2913],
        'se': [60.1282, 18.6435],
        'no': [60.4720, 8.4689],
        'dk': [56.2639, 9.5018],
        'fi': [61.9241, 25.7482],
        'pl': [51.9194, 19.1451],
        'au': [-25.2744, 133.7751],
        'br': [-14.2350, -51.9253],
        'jp': [36.2048, 138.2529],
        'cn': [35.8617, 104.1954],
        'in': [20.5937, 78.9629],
        'ru': [61.5240, 105.3188],
        'hr': [45.1000, 15.2000]
    };

    // Calculate marker sizes
    const values = Object.values(mapData.data).map(d => d.value);
    const minValue = Math.min(...values) || 0;
    const maxValue = Math.max(...values) || 1;

    // Add markers for each country
    Object.entries(mapData.data).forEach(([countryCode, countryInfo]) => {
        const coords = countryCoords[countryCode.toLowerCase()];
        if (coords) {
            const intensity = maxValue > minValue ? (countryInfo.value - minValue) / (maxValue - minValue) : 0.5;
            const size = 10 + (intensity * 20); // 10-30px radius
            const color = interpolateColor('#404040', '#1DB954', intensity);

            const marker = L.circleMarker(coords, {
                radius: size,
                fillColor: color,
                color: '#121212',
                weight: 2,
                opacity: 1,
                fillOpacity: 0.8
            }).addTo(instance.map);

            // Add tooltip
            marker.bindTooltip(`
                <div style="background: #181818; color: #ffffff; padding: 8px; border-radius: 4px; border: 1px solid #404040;">
                    <div style="font-weight: 600; color: #1DB954; margin-bottom: 4px;">
                        ${countryInfo.label.split(':')[0]}
                    </div>
                    <div style="color: #b3b3b3; font-size: 12px;">
                        ${countryInfo.label.split(':')[1] || 'No data'}
                    </div>
                </div>
            `, {
                permanent: false,
                direction: 'top'
            });
        }
    });

    console.log('Marker-based visualization created');
}

// Color interpolation function
function interpolateColor(color1, color2, factor) {
    const c1 = hexToRgb(color1);
    const c2 = hexToRgb(color2);

    const r = Math.round(c1.r + (c2.r - c1.r) * factor);
    const g = Math.round(c1.g + (c2.g - c1.g) * factor);
    const b = Math.round(c1.b + (c2.b - c1.b) * factor);

    return `rgb(${r}, ${g}, ${b})`;
}

// Convert hex to RGB
function hexToRgb(hex) {
    const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
    return result ? {
        r: parseInt(result[1], 16),
        g: parseInt(result[2], 16),
        b: parseInt(result[3], 16)
    } : { r: 0, g: 0, b: 0 };
}

// Show error message
function showMapError(canvasId, message) {
    const container = document.getElementById(canvasId);
    if (!container) {
        console.error('Container not found for error display:', canvasId);
        return;
    }

    container.innerHTML = `
        <div style="
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            height: 100%;
            background-color: #181818;
            color: #b3b3b3;
            font-family: Inter, sans-serif;
            text-align: center;
            padding: 2rem;
            border-radius: 6px;
            border: 1px solid #404040;
        ">
            <div style="font-size: 16px; margin-bottom: 8px; color: #ffffff;">
                Map Error: ${message}
            </div>
            <div style="font-size: 12px; color: #666; margin-bottom: 16px;">
                Country data will be displayed in the table below
            </div>
            <div style="font-size: 10px; color: #404040;">
                Container ID: ${canvasId}
            </div>
        </div>
    `;
}

// Cleanup function
window.destroyWorldMap = function(canvasId) {
    try {
        const instance = window.worldMapInstances[canvasId];
        if (instance) {
            // Remove Leaflet map
            if (instance.map) {
                instance.map.remove();
            }

            // Remove instance
            delete window.worldMapInstances[canvasId];
            console.log('World map destroyed:', canvasId);
        }
    } catch (error) {
        console.error('Error destroying world map:', error);
    }
};

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    console.log('Professional world map JavaScript loaded');

    // Check if Leaflet is available
    if (typeof L === 'undefined') {
        console.error('Leaflet.js is not loaded. World maps will not work.');
    } else {
        console.log('Leaflet.js is available');
    }
});

// Cleanup on page unload
window.addEventListener('beforeunload', function() {
    Object.keys(window.worldMapInstances).forEach(canvasId => {
        destroyWorldMap(canvasId);
    });
});

// Handle window resize
window.addEventListener('resize', function() {
    Object.entries(window.worldMapInstances).forEach(([canvasId, instance]) => {
        if (instance && instance.map) {
            // Leaflet handles resize automatically, just invalidate size
            setTimeout(() => {
                instance.map.invalidateSize();
            }, 100);
        }
    });
});


