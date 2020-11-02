angular.module('app')
  .factory('BaseChartConfig', ['ValueFormatter', function (ValueFormatter) {
      return {
          getPieChartOptions: function (settings) {
              settings = settings || {};
              var options = {
                  chart: {
                      type: 'pieChart',
                      height: 330,
                      width: 330,
                      margin: {
                          top: -80,
                          bottom: -50
                      },
                      showLabels: true,
                      showLegend: false,
                      duration: 500,
                      labelThreshold: 0.03,
                      labelSunbeamLayout: true
                  }
              };

              if (settings.excludeSize) {
                  delete options.chart.height;
                  delete options.chart.width;
              }

              if (settings.percentGraph) {
                  options.chart.tooltip = {
                      valueFormatter: function (d) {
                          return d + '%';
                      },
                      keyFormatter: function (d) {
                          //remove percentage sing from key if exists
                          var keys = d.split(' ');
                          if (!keys) { return ''; }

                          if (keys && keys[keys.length - 1].indexOf('%') !== -1) {
                              keys.pop();
                          }

                          return keys.join(' ');
                      }
                  };
              }

              return options;
          },
          getDonutChartConfig: function (settings) {
              var options = this.getPieChartOptions(settings);
              options.chart.donut = true;
              options.chart.donutRatio = 0.35;
              return options;
          },
          getHorizontalMultiBarOptions: function (settings) {
              settings = settings || {};

              var options = {
                  chart: {
                      type: 'multiBarHorizontalChart',
                      duration: 500,
                      groupSpacing: 0.3,
                      showLegend: true,
                      stacked: true,
                      showControls: false
                  }
              };

              if (settings.percentGraph) {
                  options.chart.tooltip = {
                      valueFormatter: function (d) {
                          return d + '%';
                      }
                  };
              }

              return options;
          },
          getDiscreteBarChartOptions: function (settings) {
              settings = settings || {};
              var options = {
                  chart: {
                      type: 'discreteBarChart',
                      margin: {
                          top: 50,
                          bottom: 100
                      },
                      height: 300,
                      width: 400,
                      duration: 500,
                      rotateLabels: -45,
                      showValues: false
                  }
              };

              if (settings.excludeSize) {
                  delete options.chart.height;
                  delete options.chart.width;
              }

              if (settings.percentGraph) {
                  options.chart.valueFormat = function (d) {
                      return ValueFormatter.toPercentageString(d);
                  };
                  options.chart.tooltip = {
                      valueFormatter: function (d) {
                          return ValueFormatter.toPercentageString(d);
                      }
                  };
                  options.chart.yAxis = {
                      tickFormat: function (d) {
                          return d + '%';
                      }
                  };
              }

              return options;
          },
          getLineChartOptions: function (settings) {
              settings = settings || {};
              return {
                  chart: {
                      type: 'lineChart',
                      height: 550,
                      duration: 500,
                      showLegend: true,
                      clipEdge: false,
                      useInteractiveGuideline: true
                  }
              }
          },
          buildBarChartTooltip: function (d, list, title, valueFormatter) {
              var openDate = '<table><thead><tr><td colspan="3"><strong class="x-value">';
              var tooltipTitle = title || '';
              var closeDate = '</strong></td></tr></thead>'
              var tbody = '<tbody>';
              var colors = d3.scale.category20().range();
              var rows = '';
              for (var i in list) {
                  var item = list[i];
                  var highlight = item.series == d.data.series ? 'class="highlight"' : '';
                  rows += '<tr ' + highlight + '> <td class="legend-color-guide" style="border-bottom-color: rgb(121, 173, 210); border-top-color: rgb(121, 173, 210);">';
                  rows += '<div style="background-color:' + colors[i % 20] + ';"></div></td>';
                  rows += '<td class="key" style="border-bottom-color: rgb(121, 173, 210); border-top-color: rgb(121, 173, 210);">';
                  rows += item.key + '</td><td class="value" style="border-bottom-color: rgb(121, 173, 210); border-top-color: rgb(121, 173, 210);">' + valueFormatter(item.y) + '</td></tr>';
              }
              var closeTable = '</tbody></table>';
              return openDate + tooltipTitle + closeDate + tbody + rows + closeTable;
          }

      };
  } ]);
