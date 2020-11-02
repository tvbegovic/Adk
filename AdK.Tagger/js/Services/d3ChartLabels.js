angular.module('app')
    .factory('d3ChartLabels', ['$timeout', 'ValueFormatter', function($timeout, ValueFormatter) {
        return {
            //settings {type: 'multiBarHorizontalChart', animationTime: 1000 }
            addLabelsToChart: function(settings) {
                switch (settings.type) {
                    case 'multiBarChart':
                        _addLabelsToStackedBarChart(settings);
                        break;
                    case 'multiBarHorizontalChart':
                        _addLabelsToMultiBarHorizontalChart(settings);
                        break;
                }
            },
            removeAllLabels: function(selector) {
                selector = selector || '';
                d3.selectAll(selector + '.bar-values-label').remove();
            }
        };

        function convertValue(value, settings) {
            value = parseFloat(value);
            switch (settings.value) {
                case 'Duration':
                    return ValueFormatter.convertSecondsToHourFormat(value);
                case 'Percentage':
                    return ValueFormatter.toPercentageString(value);
                default:
                    return ValueFormatter.toLocalString(value, true);
            }
        }

        function _addLabelsToMultiBarHorizontalChart(settings) {
            $timeout(function() {
                d3.selectAll(settings.selector + ' .nv-group').each(function() {
                    var g = d3.select(this);
                    if (!g.length) {
                        return false;
                    }

                    // Remove previous labels if there is any
                    g.selectAll('text').remove();
                    g.selectAll('.nv-bar').each(function(bar) {
                        var b = d3.select(this);
                        var rect = b.selectAll('rect');
                        var barWidth = rect.attr('width');
                        var barHeight = rect.attr('height');
                        var heightBreakpoint = 20;

                        if (b.length && barWidth > 40) {
                            var txtAtribute = g.append('text')
                                // Transforms shift the origin point then the x and y of the bar
                                // is altered by this transform. In order to align the labels
                                // we need to apply this transform to those.
                                .attr('transform', b.attr('transform'))
                                .text(function() {
                                    return convertValue(bar.y, settings);
                                })
                                .attr('y', function() {
                                    var height = (barHeight / 2) + 5;
                                    if (barHeight < heightBreakpoint) {
                                        height = height - 1;
                                    }
                                    return height; // 10 is the label's magin from the bar
                                })
                                .attr('x', function() {
                                    return barWidth < 55 ? 5 : 15;
                                })
                                .attr('class', 'bar-values-label');

                            txtAtribute.style('color', 'lightgray');


                            if (barHeight < heightBreakpoint) {
                                txtAtribute.style('font-size', '10px');
                            }
                        }

                    });
                });
            }, settings.animationTime);
        }

        function _addLabelsToStackedBarChart(settings) {
            $timeout(function() {
                d3.selectAll(settings.selector + ' .nv-group').each(function(group) {
                    var g = d3.select(this);
                    if (!g.length) {
                        return false;
                    }
                    // Remove previous labels if there is any
                    g.selectAll('text').remove();

                    g.selectAll('.nv-bar').each(function(bar) {
                        var b = d3.select(this);
                        var barWidth = b.attr('width');
                        var barHeight = b.attr('height');
                        if (barHeight > 20) {
                            g.append('text')
                                // Transforms shift the origin point then the x and y of the bar
                                // is altered by this transform. In order to align the labels
                                // we need to apply this transform to those.
                                .attr('transform', b.attr('transform'))
                                .text(function() {
                                    return convertValue(bar.y, settings);
                                })
                                .attr('y', function() {
                                    // Center label vertically
                                    var height = this.getBBox().height;
                                    return parseFloat(b.attr('y')) + 12;
                                })
                                .attr('x', function() {
                                    // Center label horizontally
                                    var width = this.getBBox().width;
                                    return parseFloat(b.attr('x')) + (parseFloat(barWidth) / 2) - (width / 2);
                                })
                                .attr('class', 'bar-values-label')
                                .style('stroke', 'black');
                        }
                    });
                });
            }, settings.animationTime);
        }
    }]);
