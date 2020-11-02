angular.module('app')
.directive('itemSelect', function () {
    return {
        restrict: 'E',
        scope: {
            itemName: '=',
            selectedItem: '=',
            loading: '=',
            placeholder: '@',
            editable: '@',
            suggestService: '=',
            itemSelected: '&',
            filter: '=',
            itemTemplateUrl: '@'
        },
        template:
        '<div>' +
        '	<input type="search" ng-model="selectedItem" class="form-control" placeholder="{{placeholder}}"' +
        '		typeahead-editable="{{editable}}" typeahead="item as item.Name for item in suggestService.suggestItems($viewValue)" typeahead-editable="false"' +
        '		typeahead-on-select="_itemSelected($item, $model, $label)" typeahead-min-length="1" typeahead-wait-ms="200" typeahead-loading="loading" typeahead-template-url="{{itemTemplateUrl}}" />' +
        '	<button type="button" class="btn btn-xs btn-primary" ng-show="canCreate()" ng-click="createItem()">Dodaj {{filter}}</button>' +
        '</div>',
        link: function (scope, element) {
            scope.editable = scope.editable && scope.suggestService.createItem;
            scope._itemSelected = function (item, model, label) {
                if (scope.itemSelected)
                    scope.itemSelected(item);
            };
            scope.canCreate = function () {
                return scope.editable && angular.isString(scope.selectedItem) && scope.selectedItem.length > 1;
            };
            scope.createItem = function () {
                scope.selectedItem = scope.suggestService.createItem(scope.selectedItem);
                scope._itemSelected(scope.selectedItem);
            };
        }
    };
})
.directive('multiSelect', function () {
    return {
        restrict: 'E',
        scope: {
            name: '@',
            items: '=',
            loading: '=',
            placeholder: '@',
            editable: '@',
            suggestService: '=',
            hideList: '=',
            createItem: '&',
            onEnter: '&',
            onAdd: '&',
            onRemove: '&'
        },
        template:
        '<ul ng-if="!hideList && items.length" class="list-unstyled"><li ng-repeat="item in items"><button type="button" class="btn btn-xs btn-primary" ng-click="removeItem(item)">Briši</button> {{suggestService.formatItem(item)}}</li></ul>' +
        '<button type="button" class="btn btn-xs btn-primary" ng-show="editable && item && item.length>1" ng-click="createItem()">Dodaj "{{item}}"</button>' +
        '<input focus-on="{{name}}" ng-keypress="keyPressed($event)" type="search" ng-model="item" class="form-control" placeholder="{{placeholder}}"' +
        '	typeahead-editable="editable" typeahead="item as suggestService.formatItem(item) for item in suggestService.suggestItems($viewValue)" typeahead-editable="false"' +
        '	typeahead-on-select="itemSelected($model)" typeahead-min-length="2" typeahead-wait-ms="200" typeahead-loading="loading" />',
        link: function (scope, element) {
            scope.item = null;
            scope.itemSelected = function (item) {
                if (item) {
                    // Exclude duplicates
                    if (scope.items.every(function (i) { return !angular.equals(i, item); }))
                        scope.items.push(item);
                    if (scope.onAdd)
                        scope.onAdd({ item: item });
                }
                scope.item = null;
            };
            scope.removeItem = function (item) {
                var index = scope.items.indexOf(item);
                if (scope.onRemove)
                    scope.onRemove({ item: item, index: index });
                scope.items.splice(index, 1);
            };
            scope.createItem = function () {
                var newItem = scope.suggestService.createItem(scope.item);
                scope.itemSelected(newItem);
            };
            scope.Except = function (list) {
                return function (item) {
                    return list.every(function (i) { return !angular.equals(i, item); });
                };
            };
            scope.keyPressed = function (event) {
                if (event.keyCode == 13) {
                    if (scope.editable && scope.item && scope.item.length > 1)
                        scope.createItem();
                    else if (scope.onEnter)
                        scope.onEnter();
                }
            };
        }
    }
})
.directive('compareTo', function () {
    return {
        require: "ngModel",
        scope: {
            otherModelValue: "=compareTo"
        },
        link: function (scope, element, attributes, ngModel) {

            ngModel.$validators.compareTo = function (modelValue) {
                return modelValue == scope.otherModelValue;
            };

            scope.$watch("otherModelValue", function () {
                ngModel.$validate();
            });
        }
    };
})
.directive('progressSeek', function () {
    return {
        restrict: 'A',
        scope: {
            onSeek: "&"
        },
        link: function (scope, element, attributes) {
            element.bind('click', function (e) {
                var totalWidth = element[0].clientWidth;
                var rect = element[0].getBoundingClientRect();
                var progressWidth = e.clientX - rect.left;
                if (scope.onSeek)
                    scope.onSeek({ pos: progressWidth / totalWidth });
            });
        }
    };
})
.provider('uiZeroclipConfig', function () {
    // default configs
    var _zeroclipConfig = {
        buttonClass: '',
        swfPath: "ZeroClipboard.swf",
        trustedDomains: [window.location.host],
        cacheBust: true,
        forceHandCursor: false,
        zIndex: 999999999,
        debug: true,
        title: null,
        autoActivate: true,
        flashLoadTimeout: 30000,
        hoverClass: "zeroclipboard-is-hover",
        activeClass: "zeroclipboard-is-active"
    };
    this.setZcConf = function (zcConf) {
        angular.extend(_zeroclipConfig, zcConf);
    };
    this.$get = function () {
        return {
            zeroclipConfig: _zeroclipConfig
        };
    };
})
.directive('uiZeroclip', ['$document', '$window', 'uiZeroclipConfig', function ($document, $window, uiZeroclipConfig) {
    var zeroclipConfig = uiZeroclipConfig.zeroclipConfig || {};
    var ZeroClipboard = $window.ZeroClipboard;
    return {
        scope: {
            onCopied: '&zeroclipCopied',
            onError: '&?zeroclipOnError',
            client: '=?uiZeroclip',
            value: '=zeroclipModel',
            text: '@zeroclipText'
        },
        link: function (scope, element, attrs) {
            // config
            ZeroClipboard.config(zeroclipConfig);
            var btn = element[0];
            if (angular.isFunction(ZeroClipboard)) {
                scope.client = new ZeroClipboard(btn);
            }
            scope.$watch('value', function (v) {
                if (v === undefined) {
                    return;
                }
                element.attr('data-clipboard-text', v);
            });
            scope.$watch('text', function (v) {
                element.attr('data-clipboard-text', v);
            });
            scope.client.on('aftercopy', _completeHnd = function (e) {
                scope.$apply(function () {
                    scope.onCopied({ $event: e });
                });
            });
            scope.client.on('error', function (e) {
                if (scope.onError) {
                    scope.$apply(function () {
                        scope.onError({ $event: e });
                    });
                }
                ZeroClipboard.destroy();
            });
            scope.$on('$destroy', function () {
                scope.client.off('complete', _completeHnd);
            });
        }
    };
}])
.config(['uiZeroclipConfigProvider', function (uiZeroclipConfigProvider) {
    // config ZeroClipboard
    uiZeroclipConfigProvider.setZcConf({
        swfPath: 'js/ZeroClipboard.swf'
    });
}])
.factory('Spreadsheet', ['$http', function ($http) {
    return function (method, data, fileName) {
        data = data || {};
        $http.post(method, data, { responseType: 'arraybuffer' })
        .success(function (response) {
            var blob = new Blob([response], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
            var link = document.createElement('a');
            link.href = window.URL.createObjectURL(blob);
            link.download = (fileName || 'export') + ".xlsx";
            link.click();
        });
    };
}])
.factory('$audioPlayer', ['$interval', 'soundmanager', 'PositionEvents', function ($interval, soundmanager, PositionEvents) {
    return function (id, scope) {
        var self = this;
        this.id = id || 'aSound';
        this.scope = scope;
        this.player = soundManager.createSound({
            id: this.id
        });
        this.destroy = function () {
            this.fastIndicator(false);
            try { this.player.destruct(); } catch (e) { }; // Error can be raised if no sound has ever been played with this player
        };
        this.seek = function (position) {
            this.player.setPosition(position);
            if (this.player.paused)
                this.togglePause();
        };
        this.play = function (options) {
            var whileplaying = options.whileplaying;
            options.whileplaying = function () {
                self.fastIndicator(true);
                self.scope.$apply(function () {
                    self.position = self.player.position;
                });
                if (whileplaying)
                    whileplaying();
            };

            this.player.play(options);
        };
        this.fastIndicatorInterval = 20;
        this.fastIndicator = function (bStart) {
            if (bStart) {
                this.fastIndicator(false);
                this.interval = $interval(function (i) {
                    self.position = self.player.position + i * self.fastIndicatorInterval;
                    //console.log('pos: ' + self.position);
                }, this.fastIndicatorInterval);
            } else if (this.interval) {
                $interval.cancel(this.interval);
                this.interval = null;
            }
        };
        this.stop = function () {
            if (this.player.playState == 1) {
                this.player.stop();
                this.fastIndicator(false);
            }
        };
        this.pause = function () {
            if (!this.player.paused)
                this.player.pause();
            this.fastIndicator(false);
        };
        this.togglePause = function () {
            this.player.togglePause();
            this.fastIndicator(!this.player.paused);
        };
    };
}])
.factory('getIndustry', ['Service', function (Service) {
    return {
        suggestItems: function (val) {
            return Service('GetIndustries', { q: val });
        },
        formatItem: function (item) {
            return item ? item.Name + (item.Id == 0 ? ' (new)' : '') : '';
        },
        createItem: function (itemName) {
            return { Id: null, Name: itemName }; // Create an industry with ID=null, to later be created server-side
        }
    };
}])
.factory('getCompany', ['Service', function (Service) {
    return {
        suggestItems: function (val) {
            return Service('GetCompanies', { q: val });
        },
        formatItem: function (item) {
            return item ? item.Name + (item.Id == 0 ? ' (new)' : '') : '';
        },
        createItem: function (itemName) {
            return { Id: null, Name: itemName }; // Create a company with ID=null, to later be created server-side
        }
    };
}])
.factory('getTag', ['Service', function (Service) {
    return {
        suggestItems: function (val) {
            return Service('GetTags', { q: val });
        },
        suggestItemsWithUsage: function (prefix, val, withBrands, withCompanies) {
            return Service('GetTagsUse', { prefix: prefix, q: val, withBrands: withBrands, withCompanies: withCompanies });
        },
        formatItem: function (item) {
            return item ? item.Name + (item.Id == 0 ? ' (new)' : '') : '';
        },
        createItem: function (itemName) {
            return { Id: null, Name: itemName }; // Create a tag with ID=null, to later be created server-side
        }
    };
}])
.factory('markers', [function () {
    var _m = {
        isopen: false,
        allValues: ['music', 'SFX', 'English', 'singing', 'female', 'male', 'child'],
        active: 0,
        onKeyDown: function (event, text) {
            if (event.keyCode === 219) // ']'
                _m.isopen = false;
            else {
                if (event.keyCode === 40 && _m.active < _m.filteredValues.length - 1) // Down
                    ++_m.active;
                if (event.keyCode === 38 && _m.active > 0) // Up
                    --_m.active;
                if (event.keyCode === 8) // Backspace
                    _m.filter(text.length > 0 ? text.substr(0, text.length - 1) : text);
            }
        },
        filter: function (text) {
            _m.prefix = text.match(/\[([^\[\]]*)$/i); // '[' followed by n times not '[' nor ']'
            if (_m.prefix != null) _m.prefix = _m.prefix[1]; // First capturing parenthesis
            _m.filteredValues = _.filter(_m.allValues, _m.matches);
            if (_m.prefix == null || _m.filteredValues == 0)
                _m.isopen = false;
            _m.active = Math.max(0, Math.min(_m.active, _m.filteredValues.length - 1));
        },
        matches: function (marker) {
            return !_m.prefix || _m.prefix.length === 0 || marker.substr(0, _m.prefix.length).toLowerCase() == _m.prefix.toLowerCase();
        },
        insert: function (text) {
            var inserted = '';
            if (_m.filteredValues.length > 0) {
                var value = _m.filteredValues[_m.active];
                if (_m.prefix != null) {
                    inserted += value.substr(_m.prefix.length) + '] '
                } else {
                    if (text.length > 0 && text[text.length - 1] !== ' ')
                        inserted += ' ';
                    inserted += '[' + value + '] ';
                }
            }
            return inserted;
        },
    };
    return _m;
}])
.directive('tagButton', function () {
    return {
        restrict: 'E',
        replace: true,
        templateUrl: 'tagButtonTemplate.html',
        scope: {
            tag: '=',
            toggled: '&onToggle',
        },
    };
})
.directive('indeterminate', [function () {
    return {
        require: '?ngModel',
        link: function (scope, el, attrs, ctrl) {
            var truthy = true;
            var falsy = false;
            var nully = null;
            ctrl.$formatters = [];
            ctrl.$parsers = [];
            ctrl.$render = function () {
                var d = ctrl.$viewValue;
                el.data('checked', d);
                switch (d) {
                    case truthy:
                        el.prop('indeterminate', false);
                        el.prop('checked', true);
                        break;
                    case falsy:
                        el.prop('indeterminate', false);
                        el.prop('checked', false);
                        break;
                    default:
                        el.prop('indeterminate', true);
                }
            };
            el.bind('click', function () {
                var d;
                switch (el.data('checked')) {
                    case falsy:
                        d = truthy;
                        break;
                    case truthy:
                        d = nully;
                        break;
                    default:
                        d = falsy;
                }
                ctrl.$setViewValue(d);
                scope.$apply(ctrl.$render);
            });
        }
    };
}])
.directive('datePicker', function () {
    return {
        templateUrl: 'datePicker.html',
        scope: {
            date: '=',
            datePicked: '&',
            alignRight: '='
        },
        link: function (scope, element, attrs) {
            scope.isOpened = false;
            scope.now = new Date();
            scope.open = function ($event) {
                $event.preventDefault();
                $event.stopPropagation();

                scope.isOpened = true;
            };

            //scope.dateOptions = {
            //	formatYear: 'yy',
            //	initDate: new Date(),
            //	startingDay: 1
            //};
        }
    };
})
.directive('transcript', function () {
    return {
        restrict: 'E',
        templateUrl: 'view/transcript.html',
        controller: 'transcriber',
        scope: {
            isTraining: '=',
            isReview: '='
        }
    };
})
.controller('mainCtrl', ['$scope', '$q', 'Service', 'Authenticate', 'soundmanager', 'focus', 'getIndustry', 'getCompany', 'getTag', function ($scope, $q, Service, Authenticate, soundmanager, focus, getIndustry, getCompany, getTag) {
    var initFinished = false;
    var player = null;
    $q.all([Authenticate.promise(), soundmanager]).then(function () {
        player = soundManager.createSound({
            id: 'aSound',
            loops: 5 // listen to the sample 5 times
        });
        getProducts();
    }, function (reason) {
        alert('Failed: ' + reason);
    }, function (update) {
        alert('Got notification: ' + update);
    });
    $scope.play = function (options) {
        if (player.playState == 1)
            player.stop();
        player.play(options);
    }
    $scope.stop = function () {
        if (player.playState == 1)
            player.stop();
    }
    $scope.$on('$destroy', function () {
        $scope.stop();
        player.destruct();
    });

    $scope.getCompany = getCompany;
    $scope.getIndustry = getIndustry;
    $scope.getTag = getTag;

    $scope.editing = {
        industryFilter: '',
        tagFilter: '',
        companyFilter: ''
    };

    $scope.filter = {
        active: false,
        titleFilter: '',
        tags: [],
        searchResults: null
    };
    $scope.$watch('filter.titleFilter', function (newFilter) {
        $scope.filter.searchResults = null;
        if ($scope.filter.visible && newFilter)
            Service('SearchSamples', { filter: newFilter }).then(function (res) {
                if ($scope.filter.titleFilter == newFilter)
                    $scope.filter.searchResults = res;
            });
    });

    function getProducts() {
        Service('GetProducts').then(function (products) {
            $scope.products = products;
            initFinished = true;
            loadNext();
        });
    }
    $scope.selectProduct = function (product) {
        $scope.selectedProduct = product;
    };
    $scope.getNewEntities = function () {
        Service('GetNewEntities').then(function (res) {
            $scope.gotNew = {
                industries: res[0],
                companies: res[1],
                tags: res[2],
                categories: res[3]
            };
        });
    };
    function loadNext() {
        if (!initFinished)
            return;
        $scope.stop();
        Service('LoadNext', { filter: getFilter() }).then(gotNext);
    }
    function gotNext(song) {
        $scope.song = song;
        if (song != null) {
            song.Tags = [];
            if ($scope.filter.active)
                song.Tags = song.Tags.concat($scope.filter.tags);
            $scope.selectedProduct = $scope.products[0];
            $scope.editing.industryFilter = null;
            $scope.editing.companyFilter = null;
            $scope.editing.tagFilter = null;
            $scope.editing.industry = true;
            $scope.editing.company = true;
            $scope.editing.tag = true;

            if (!$scope.filter.visible)
                focus('tagSearch');

            $scope.play({ url: $scope.song.FilePath });
        }


        getStatistics();
    }
    function getStatistics() {
        Service('GetVoteStatistics', { filter: getFilter() }).then(function (stats) {
            $scope.statistics = stats;
        });
    }
    $scope.submit = function () {
        if ($scope.song.Tags.length == 0)
            $scope.skip();
        else {
            var data = {
                songId: $scope.song.Id,
                tags: $scope.song.Tags,
                company: $scope.song.Company || null,
                industry: $scope.song.Industry || null,
                category: null,
                productId: $scope.selectedProduct.Id || null,
                filter: getFilter()
            };
            Service('Vote', data).then(gotNext);
        }
    };
    $scope.skip = function () {
        Service('Skip', {
            songId: $scope.song.Id,
            reason: 1,
            filter: getFilter()
        }).then(gotNext);
    };
    function getFilter() {
        return ($scope.filter.active ? $scope.filter.titleFilter : null) || null;
    }
    $scope.$watchGroup(['filter.active', 'filter.titleFilter'], function (newActive) {
        loadNext();
    });
}])
.controller('dashboardCtrl', ['$scope', 'Service', function ($scope, Service) {
    $scope.dash = {};
    var dash = $scope.dash;
    function reload() {
        if ($scope.user.isAdmin || $scope.user.granted['transcript']) {
            dash.transcript = {};
            Service('GetTranscriptQueueLength', { training: false, review: false }).then(function (queueLength) { dash.transcript.queueLength = queueLength; });
            //getTranscriptStatisticsChart();
            getTranscriptEarnings();
        }
    }
    function getTranscriptStatisticsChart() {
        Service('GetTranscriptStatisticsChart').then(function (performanceChart) {
            dash.transcript.performanceChartStart = moment(performanceChart[0].Day);
            dash.transcript.performanceChart = performanceChart.map(function (item) {
                return {
                    Day: moment(item.Day).diff(dash.transcript.performanceChartStart, 'days'),
                    Performance: item.Performance
                }
            });
        });

        var daySpan = 30;
        $scope.options = {
            axes: {
                x: { key: 'Day', labelFunction: function (d) { return moment(dash.transcript.performanceChartStart).add(d, 'days').format('dd'); }, min: 0, max: daySpan - 1, ticks: daySpan },
                y: { type: 'linear', min: 1 }
            },
            series: [
            { y: 'Performance', color: 'steelblue', thickness: '2px', type: 'line', striped: true, label: 'Performance' }
            ],
            lineMode: 'basis',
            tension: 0.9,
            tooltip: { mode: 'scrubber', formatter: function (x, y, series) { return Math.round(y * 10) / 10; } },
            drawLegend: false,
            drawDots: true
        };
    }
    function getTranscriptEarnings() {
        Service('GetTranscriptEarnings').then(function (stats) {
            if (stats) {
                stats.forEach(function (stat) {
                    stat.Period = moment(stat.Start).format('l') + ' - ' + moment(stat.End).add(-1, 'day').format('l');
                    stat.Month = moment(stat.Start).format('MMMM');
                });
                $scope.earningByMonth = [stats[0], stats[1]];
                $scope.earningByWeek = [stats[2], stats[3], stats[4], stats[5]];
            }
        });
    }
    reload();
}])
.controller('tagManagerCtrl', ['$scope', 'Service', '$modal', '$window', 'getTag', 'soundmanager', function ($scope, Service, $modal, $window, getTag, soundmanager) {
    $scope.editing = {
        prefixes: 'abcčćdefghijklmnoprsštuvzž',
        prefix: '',
        tagFilter: '',
        withBrands: null,
        withCompanies: null
    };
    $scope.manager = { split: {}, merged: {}, selected: [], attributes: [], suggested: [] };
    var mgr = $scope.manager;
    $scope.getTag = getTag;

    $scope.$watch('manager.tag.Id', function (tagId) {
        if (tagId)
            tagSelected(tagId);
    });
    $scope.searchTags = function () {
        $scope.processing = true;
        getTag.suggestItemsWithUsage($scope.editing.prefix, $scope.editing.tagFilter, $scope.editing.withBrands, $scope.editing.withCompanies).then(function (tags) {
            $scope.tags = tags;
            tags.forEach(function (tag) {
                var pos = -1;
                mgr.selected.some(function (t, i) {
                    if (t.Id == tag.Id)
                        pos = i;
                    return pos > -1;
                });
                if (pos > -1) {
                    tag.selected = true;
                    mgr.selected.splice(pos, 1, tag); // Replace with the tag from the new list
                }
            });
            $scope.processing = false;
        });
    };
    $scope.clearSearch = function () {
        $scope.editing.tagFilter = '';
        $scope.searchTags();
    }

    $scope.height = {};
    function windowResized() {
        var windowHeight = $window.innerHeight;
        var headerHeight = 120;
        $scope.height.belowHeader = windowHeight - headerHeight;
    }
    angular.element($window).on('resize', function () { $scope.$apply(windowResized); });
    windowResized();

    $scope.tagToggled = function (tag) {
        if (tag.selected)
            mgr.selected.push(tag);
        else {
            var pos = mgr.selected.indexOf(tag);
            if (pos > -1)
                mgr.selected.splice(pos, 1);
        }
        if (mgr.selected.length > 0) {
            mgr.merged.master = mgr.selected[mgr.selected.length - 1];
            mgr.split.name1 = mgr.merged.master.Name;
            mgr.split.name2 = mgr.merged.master.Name;
            mgr.merged.name = mgr.merged.master.Name;
            tagSelected(mgr.merged.master.Id);
            if (mgr.selected.length > 0)
                getTagAttributes();
        }
    };
    $scope.removeTag = function (tag) {
        tag.selected = false;
        $scope.tagToggled(tag);
    };
    function tagSelected(tagId) {
        mgr.tagSongs = null;
        mgr.tagCreator = null;
        $scope.stop();
        if (tagId)
            Service('GetTaggedSamples', { tagId: tagId }).then(function (res) {
                mgr.tagSongs = res.samples;
                mgr.tagCreator = res.creator;
            });
    };
    $scope.canSplit = function () {
        return mgr.split.name1 && mgr.split.name2 && mgr.split.name1 != mgr.split.name2;
    };
    $scope.split = function () {
        Service('SplitTags', {
            masterId: mgr.merged.master.Id,
            name1: mgr.split.name1,
            name2: mgr.split.name2
        }).then(function (res) {
            if (res) {
                mgr.merged.master.Name = mgr.split.name1;
                function matchesFilter(filter, tag) { return filter === '' || tag.Name.toLowerCase().indexOf(filter.toLowerCase()) != -1; }
                if (matchesFilter($scope.editing.tagFilter, res))
                    $scope.tags.push(res);
                mgr.selected = [mgr.merged.master];

                getStatistics();
            }
        });
    };
    $scope.merge = function () {
        mgr.merged.slaveIds = mgr.selected.map(function (tag) { return tag.Id; });
        // last item is the master tag
        mgr.merged.slaveIds.length = mgr.merged.slaveIds.length - 1;

        Service('MergeTags', {
            masterId: mgr.merged.master.Id,
            slaveIds: mgr.merged.slaveIds,
            name: mgr.merged.name
        }).then(function (res) {
            if (!res)
                return;

            mgr.merged.master.Name = mgr.merged.name;

            mgr.selected.length = mgr.selected.length - 1;
            mgr.selected.forEach(removeTagFromColumn);
            mgr.selected = [];
            tagSelected(null);

            $scope.searchTags();
            getStatistics();
        });
    };
    function removeTagFromColumn(tag) {
        var tags = $scope.tags;
        var slaveIndex = -1;
        for (var i = 0; i < tags.length && slaveIndex === -1; ++i)
            if (tags[i].Id == tag.Id)
                slaveIndex = i;
        if (slaveIndex != -1)
            tags.splice(slaveIndex, 1);
    }
    function getStatistics() {
        Service('GetTagStatistics').then(function (stats) {
            $scope.statistics = stats;
        });
    }

    // Initially show all tags
    $scope.searchTags();
    getStatistics();

    var player = null;
    var playedSong = null;
    soundmanager.then(function () {
        player = soundManager.createSound({
            id: 'aSound',
            loops: 5, // listen to the sample 5 times
        });
    });
    $scope.play = function (song) {
        $scope.stop();
        playedSong = song;
        player.play({ url: song.FilePath });
    }
    $scope.stop = function () {
        if (player.playState == 1) {
            player.stop();
            playedSong = null;
        }
    }
    $scope.isPlaying = function (song) {
        return player.playState == 1 && song === playedSong;
    };
    $scope.$on('$destroy', function () {
        $scope.stop();
        player.destruct();
    });

    $scope.getAttribute = {
        suggestItems: function (q) {
            return Service('SuggestTagAttributes', { q: q });
        },
        formatItem: function (item) {
            if (!item) return '';
            return item.Name + (item.Guid == 0 ? ' (new)' : '') + ' (' + item.Type + ')';
        },
        createItem: function (itemName) {
            var attribute = { Guid: null, Name: itemName }; // Create an attribute with ID=null, to later be created server-side

            $modal.open({
                animation: false,
                templateUrl: 'attributeKindModal.html',
                controller: function ($scope, $modalInstance) {
                    $scope.createBrand = function () {
                        $modalInstance.close('Brand');
                    };
                    $scope.createAdvertiser = function () {
                        $modalInstance.close('Company');
                    };
                },
                size: 'sm'
            }).result.then(function (attributeType) {
                attribute.Type = attributeType;
                Service('CreateTagAttribute', { attribute: attribute }).then(function (res) {
                    attribute.Guid = res.Guid;
                    mgr.attributes.push(attribute);
                    $scope.onAddedAttribute(attribute);
                });
            });
        }
    };
    $scope.onAddedAttribute = function (attribute) {
        var tagIds = mgr.selected.map(function (tag) { return tag.Id; });
        Service('AddTagAttribute', { tagIds: tagIds, attribute: attribute });
        for (var i in mgr.selected)
            updateTagAttribute(mgr.selected[i], attribute, +1);
    };
    $scope.onRemovingAttribute = function (attribute, index) {
        var tagIds = mgr.selected.map(function (tag) { return tag.Id; });
        Service('RemoveTagAttribute', { tagIds: tagIds, attribute: attribute });
        for (var i in mgr.selected)
            updateTagAttribute(mgr.selected[i], attribute, -1);
    };
    function getTagAttributes(tag) {
        mgr.suggested = [];
        mgr.attributes = [];
        var tagIds = mgr.selected.map(function (tag) { return tag.Id; });
        Service('GetTagAttributes', { tagIds: tagIds }).then(function (res) {
            mgr.suggested = res; // Fixes bug in ui-select where initial 'matches' must also be in 'choices'
            mgr.attributes = res;
        });
    };
    function updateTagAttribute(tag, attribute, delta) {
        switch (attribute.Type) {
            case 'Brand':
                tag.Attributes[1] += delta; break;
            case 'Company':
                tag.Attributes[2] += delta; break;
        }
    }
}])
.factory('PositionEvents', [function () {
    function PositionEvents() {
        var _p = this;
        this.player = null;
        this.list = [];
        this.add = function (time, fn, sc) {
            time = Math.round(time);
            this.player.onPosition(time, fn, sc);
            this.list.push(time);
        };
        this.addVolume = function (time, volume) {
            volume = Math.round(volume);
            this.add(time, function () {
                _p.player.setVolume(volume);
            }, { volume: volume });
        };
        this.overlap = 1000; // milliseconds, volume 100%
        this.fadeOutDuration = 350;
        this.fadeInDuration = 350;
        this.fadeOutSteps = Math.floor(this.fadeOutDuration / 80);
        this.fadeInSteps = Math.floor(this.fadeInDuration / 80);
        this.fadeIn = function (timeStart) {
            for (var i = this.fadeInSteps; i >= 1; --i)
                this.addVolume(timeStart - this.fadeInDuration * (1 - i / this.fadeInSteps), 100 * i / this.fadeInSteps);
        };
        this.fadeOut = function (timeEnd) {
            for (var i = this.fadeOutSteps; i >= 1; --i)
                this.addVolume(timeEnd + this.overlap + this.fadeOutDuration * (i - 1) / this.fadeOutSteps, 100 * (1 - i / this.fadeOutSteps));
        };
        this.clearAll = function () {
            for (var iPos in this.list)
                this.player.clearOnPosition(this.list[iPos]);
            this.list = [];
        };
    }
    return PositionEvents;
}])
.controller('transcriber', ['$scope', '$q', 'Service', 'Authenticate', 'soundmanager', 'PositionEvents', 'focus', 'markers', function ($scope, $q, Service, Authenticate, soundmanager, PositionEvents, focus, markers) {
    var player = null;
    var positionEvents = new PositionEvents();
    $q.all([Authenticate.promise(), soundmanager]).then(loadNext);
    function play() {
        stop();

        var options = {
            url: $scope.song.FilePath,
            from: $scope.part.TimeStart,
            volume: 100,
            whileplaying: function () {
                positionChanged(player.position);
            }
        };
        if (!isFirstPart()) {
            options.from -= positionEvents.fadeInDuration;
            options.volume = 0;
            positionEvents.fadeIn($scope.part.TimeStart);
        }
        if (isLastPart()) {
            options.onfinish = function () {
                positionChanged($scope.part.TimeEnd);
                play();
            };
        } else {
            positionEvents.add($scope.part.TimeEnd + positionEvents.fadeOutDuration + positionEvents.overlap, play);
            positionEvents.fadeOut($scope.part.TimeEnd);
        }

        player.play(options);
        $scope.isPaused = false;
    }
    function stop() {
        positionEvents.clearAll();
        if (player && player.playState == 1)
            player.stop();
        $scope.isPaused = true;
    }
    $scope.pause = function () {
        player.togglePause();
        $scope.isPaused = player.paused;
    };
    $scope.$on('$destroy', function () {
        stop();
        player.destruct();
    });

    function loadNext() {
        stop();
        Service('LoadNextTranscript', { training: $scope.isTraining || false, review: $scope.isReview || false }).then(gotNext);
    }
    function gotNext(song) {
        getQueueLength();
        $scope.song = song;

        if (song) {
            player = soundManager.createSound({
                id: 'aSound'
            });
            positionEvents.player = player;

            $scope.iPart = 0;
            startPartEdit();
            focus('text');
        }
    }

    $scope.skip = loadNext;

    $scope.markers = markers;
    $scope.lastKeyCode = null;
    $scope.onKeyDown = function (event) {
        if (event.ctrlKey && event.keyCode === 66) { // Ctrl+b
            event.preventDefault();
            markers.isopen = !markers.isopen;
            if (markers.isopen)
                $scope.part.Text += '[';
        }
        if (!markers.isopen && event.keyCode === 53) { // '['
            markers.isopen = true;
        }
        if (markers.isopen) {
            markers.onKeyDown(event, $scope.part.Text);
            if (event.keyCode === 13)
                $scope.part.Text += markers.insert($scope.part.Text);
        } else {
            if (event.keyCode === 13 && isLastPart()) {
                submitTranscript();
            } else if (event.keyCode === 8 && $scope.part.Text.length === 0) { // Backspace
                event.preventDefault();
                previousPart();
            } else if ($scope.lastKeyCode === 32 && event.keyCode === 32) { // Two consecutive spaces
                event.preventDefault();
                $scope.lastKeyCode = null;
                nextPart();
            } else if (event.keyCode === 40 && $scope.isReview) { // Down arrow
                nextPart();
            } else if (event.keyCode === 38 && $scope.isReview) { // Up arrow
                previousPart();
            } else
                $scope.lastKeyCode = event.keyCode;
        }
    };
    $scope.onKeyUp = function (event) {
        if (markers.isopen)
            markers.filter($scope.part.Text);
    }
    $scope.insertMarker = function (index) {
        markers.active = index;
        $scope.part.Text += markers.insert($scope.part.Text);
    };

    function isFirstPart() {
        return $scope.iPart === 0;
    }
    function isLastPart() {
        return $scope.iPart === $scope.song.Parts.length - 1;
    }
    function startPartEdit(leaveText) {
        $scope.part = $scope.song.Parts[$scope.iPart];
        $scope.part.EditStart = new Date(Date.now()).toISOString();
        if (!$scope.isReview && !leaveText)
            $scope.part.Text = '';
        play();
    }
    function endPartEdit() {
        $scope.part.EditEnd = new Date(Date.now()).toISOString();
    }
    function nextPart() {
        endPartEdit();
        if (!isLastPart()) {
            $scope.iPart++;
            startPartEdit();
        }
    }
    function previousPart() {
        if ($scope.iPart > 0) {
            $scope.iPart--;
            startPartEdit(true);
        }
    }
    function positionChanged(position) {
        $scope.$apply(function () {
            $scope.position = Math.max($scope.part.TimeStart, Math.min($scope.part.TimeEnd, position));
        });
    }
    function submitTranscript() {
        if ($scope.song) {
            stop();
            soundManager.destroySound('aSound');
            endPartEdit();
            buildStats();
            Service('SaveTranscript', { transcript: $scope.song }).then(gotNext);
            $scope.song = null; // Prevents saving again while save is in progress
        }
    }

    $scope.submits = [];
    function buildStats() {
        var start = moment($scope.song.Parts[0].EditStart);
        var end = moment($scope.song.Parts[$scope.song.Parts.length - 1].EditEnd);
        var transcriptDuration = moment.duration(end.diff(start), 'ms');
        var songDuration = moment.duration($scope.song.Duration, 'ms');
        var text = $scope.song.Parts.map(function (p) { return p.Text; }).join(' ').replace(/\s+/g, ' ');
        $scope.submits.push({
            Text: text,
            SongDuration: songDuration,
            TranscriptDuration: transcriptDuration,
            Performance: transcriptDuration.asSeconds() / songDuration.asSeconds(),
            ClipboardText: text + '\r\nhttp://accessa.streamsink.com/clients/mediainsite/view.asp?v=' + $scope.song.PksId.replace(/-/g, '')
        });

        buildSummaryStats();
    }
    function buildSummaryStats() {
        $scope.submitsEditTime = $scope.submits.reduce(function (sum, submit) { return sum.add(submit.TranscriptDuration); }, moment.duration(0));
        $scope.submitsDuration = $scope.submits.reduce(function (sum, submit) { return sum.add(submit.SongDuration); }, moment.duration(0));
        $scope.submitsRatio = $scope.submitsEditTime.asSeconds() / $scope.submitsDuration.asSeconds();
    }
    function getQueueLength() {
        Service('GetTranscriptQueueLength', { training: $scope.isTraining || false, review: $scope.isReview || false }).then(function (queueLength) { $scope.queueLength = queueLength; });
    }
    $scope.restartTraining = function () {
        Service('RestartTraining').then(loadNext);
    };
}])
.controller('transcriberStatisticsCtrl', ['$scope', '$filter', '$timeout', 'Service', 'Authenticate', 'fs', function ($scope, $filter, $timeout, Service, Authenticate, fs) {
    Authenticate.promise().then(loadStatistics);

    $scope.filter = {
        dateStart: null,
        dateEnd: null
    };
    $scope.datesChanged = function () {
        $timeout(function () { // delaying as datesChanged fires before actual values are changed
            if (!$scope.filter.dateStart) {
                $scope.filter.dateEnd = null;
            } else {
                if (!$scope.filter.dateEnd || moment($scope.filter.dateEnd).isBefore($scope.filter.dateStart)) {
                    $scope.filter.dateEnd = moment($scope.filter.dateStart).toDate(); // date picker expects a JS date
                }
            }
            loadStatistics();
        });
    };

    function receiveStats(stats) {
        stats.forEach(function (stat) {
            stat.Day = moment(stat.Day);
        });
        return stats;
    }
    function getTranscriptStatistics(userId, dateStart, dateEnd) {
        var start = dateStart ? moment(dateStart).format('YYYY-MM-DD') : null;
        var end = dateEnd ? moment(dateEnd).add(1, 'day').format('YYYY-MM-DD') : null; // add one day as select uses date >= dateStart AND date < dateEnd

        return Service('GetTranscriptStatistics', { userId: userId, dateStart: start, dateEnd: end }).then(function (stats) {
            return receiveStats(stats);
        });
    }

    function loadStatistics() {
        if (!$scope.selectedUser)
            getTranscriptStatistics(null, $scope.filter.dateStart, $scope.filter.dateEnd).then(function (stats) {
                $scope.stats = stats;

                var statsGrid = new fs.Grid(stats);
                new fs.Column(statsGrid, 'User Name', 'Name');
                new fs.Column(statsGrid, 'Email', 'Email');
                new fs.Column(statsGrid, 'Samples', 'SongCount');
                new fs.Column(statsGrid, 'Edit time', 'EditTime');
                new fs.Column(statsGrid, 'Total duration', 'SongDuration').displayFilter(function (duration) { return $filter('number')(duration, 1); });
                new fs.Column(statsGrid, 'Performance', 'Performance').displayFilter(function (performance) { return $filter('number')(performance, 1); });
                $scope.statsGrid = statsGrid;

                $scope.orderByStatsGrid = function (stat) {
                    return $scope.statsGrid.orderKey(stat);
                };
            });
        else {
            if (!$scope.selectedUser.date)
                getTranscriptStatistics($scope.selectedUser.Id, $scope.filter.dateStart, $scope.filter.dateEnd).then(function (stats) {
                    $scope.selectedUser.stats = stats;

                    var grid = new fs.Grid(stats);
                    new fs.Column(grid, 'Date', 'Day').displayFilter(function (date) { return date.format('DD.MM.YYYY'); });
                    new fs.Column(grid, 'Song count', 'SongCount');
                    new fs.Column(grid, 'Edit time', 'EditTime');
                    new fs.Column(grid, 'Total duration', 'SongDuration').displayFilter(function (duration) { return $filter('number')(duration, 1); });
                    new fs.Column(grid, 'Performance', 'Performance').displayFilter(function (performance) { return $filter('number')(performance, 1); });
                    $scope.statsDateGrid = grid;

                    $scope.orderByStatsDateGrid = function (stat) {
                        return $scope.statsDateGrid.orderKey(stat);
                    };
                });
            else
                getTranscriptStatistics($scope.selectedUser.Id, $scope.selectedUser.date.Day, null).then(function (stats) {
                    $scope.selectedUser.date.stats = stats;

                    var grid = new fs.Grid(stats);
					new fs.Column(grid, 'Date', 'Day').displayFilter(function (date)
					{
						return date.format('HH:mm:ss');
					});
                    new fs.Column(grid, 'Edit time', 'EditTime');
                    new fs.Column(grid, 'Total duration', 'SongDuration').displayFilter(function (duration) { return $filter('number')(duration, 1); });
                    new fs.Column(grid, 'Performance', 'Performance').displayFilter(function (performance) { return $filter('number')(performance, 1); });
                    new fs.Column(grid, 'Text', 'Text');
                    $scope.statsUserGrid = grid;

                    $scope.orderByStatsUserGrid = function (stat) {
                        return $scope.statsUserGrid.orderKey(stat);
                    };
                });
        }
    }
    $scope.getUserStatistics = function (stat) {
        $scope.selectedUser = { Id: stat.UserId, Email: stat.Email, Name: stat.Name };
        loadStatistics();
    };
    $scope.cancelUser = function () {
        $scope.selectedUser = null;
    };
    $scope.getUserDateStatistics = function (stat) {
        $scope.selectedUser.date = { Day: stat.Day };
        loadStatistics();
    };
    $scope.cancelDate = function () {
        $scope.selectedUser.date = null;
    };
}])
.factory('transcriptManagerFilter', ['LocalStorage', function (LocalStorage) {
    var _f = {
        _key: 'transcriptManagerFilter',
        filter: null,
        save: function () {
            LocalStorage.setJson(_f._key, _f.filter);
        }
    };
    _f.filter = angular.extend({
        status: null,
        startDate: moment().add(-6, 'months').format('YYYY-MM-DD'),
        endDate: moment().format('YYYY-MM-DD'),
        text: null,
        sort: {
            ascending: true,
            column: 'created'
        }
    }, LocalStorage.getJson(_f._key));
    return _f;
}])
.constant('transcriptStatuses', {
    None: 0,
    CorrectionPending: 1,
    Corrected: 2,
    Trashed: 3
})
.directive('transcriptStatusIcon', function () {
    return {
        scope: {
            transcript: '='
        },
        templateUrl: 'transcriptStatusIcon.html',
        controller: ['$scope', 'transcriptStatuses', function ($scope, transcriptStatuses) {
            $scope.statuses = transcriptStatuses;
        }]
    };
})
.filter('priorityLabel', function () {
    return function (priority) {
        if (priority >= 100)
            return 'Immediate';
        if (priority >= 50)
            return 'Normal';
        return 'Idle';
    };
})
.service('transcriptSelection', function () {
    return _this = {
        current: {},
        clear: function () {
            Object.keys(_this.current).forEach(function (key) { delete _this.current[key]; });
        },
        getIds: function () {
            return _.filter(Object.keys(_this.current), function (songId) { return _this.isSelected(songId); });
        },
        isSelected: function (songId) {
            return songId in _this.current && _this.current[songId];
        },
        count: function () {
            return _this.getIds().length;
        },
        add: function (songId) {
            _this.current[songId] = true;
        },
        remove: function (songId) {
            if (songId in _this.current)
                delete _this.current[songId];
        },
        set: function (songId, selected) {
            if (selected)
                _this.add(songId);
            else
                _this.remove(songId);
            return selected;
        },
        toggle: function (songId) {
            return _this.set(songId, !_this.isSelected(songId));
        }
    };
})
.controller('transcriptManagerCtrl', ['$scope', '$timeout', 'Service', 'soundmanager', 'transcriptManagerFilter', 'transcriptStatuses', 'OrderTranscriptsWindow', 'confirmPopup', 'Pager', 'transcriptSelection',
function ($scope, $timeout, Service, soundmanager, transcriptManagerFilter, transcriptStatuses, OrderTranscriptsWindow, confirmPopup, Pager, transcriptSelection) {
    $scope.filter = transcriptManagerFilter.filter;
    $scope.statuses = transcriptStatuses;
    var commonFilters = [
    { label: "(All)", value: null },
    { label: "(Blanks)", value: false },
    { label: "(Non blanks)", value: true }
    ];
    $scope.pager = new Pager();
    $scope.setProductFilter = function (product) {
        $scope.filter.product = product.value;
        getSamples();
    }
    $scope.setCategoryFilter = function (category) {
        $scope.filter.category = category.value;
        getSamples();
    }
    $scope.setCompanyFilter = function (company) {
        $scope.filter.company = company.value;
        getSamples();
    }
    $scope.setBrandFilter = function (brand) {
        $scope.filter.brand = brand.value;
        getSamples();
    }
    $scope.setSort = function (column) {
        if ($scope.filter.sort.column == column) {
            $scope.filter.sort.ascending = !$scope.filter.sort.ascending;
        } else {
            $scope.filter.sort.column = column;
            $scope.filter.sort.ascending = true;
        }
        getSamples();
    };
    $scope.datesChanged = function () {
        $timeout(function () { // delaying as datesChanged fires before actual values are changed
            if ($scope.filter.startDate)
                $scope.filter.startDate = moment($scope.filter.startDate).format('YYYY-MM-DD');
            if ($scope.filter.endDate)
                $scope.filter.endDate = moment($scope.filter.endDate).format('YYYY-MM-DD');
            if ($scope.filter.startDate && $scope.filter.endDate && moment($scope.filter.startDate).isAfter($scope.filter.endDate))
                $scope.filter.endDate = $scope.filter.startDate;
            getSamples();
        });
    };
    $scope.statusChanged = getSamples;
    $scope.textChanged = getSamples;

    function getSamples(keepPagerIndex) {
        if (!$scope.distinctFiltersLoaded) return;
        if (!keepPagerIndex) {
            $scope.pager.reset();
            transcriptSelection.clear();
        }
        transcriptManagerFilter.save();
        Service('GetSamples', {
            pageNum: $scope.pager.index - 1,
            pageSize: $scope.pager.size,
            sortColumn: $scope.filter.sort.column,
            ascending: $scope.filter.sort.ascending,
            filter: $scope.filter
        }).then(function (res) {
            res.samples.forEach(function (sample) {
                sample.Created = sample.Created == null ? null : moment(sample.Created);
                updateSampleStatus(sample);
            });
            $scope.samples = res.samples;
            $scope.pager.setItemCount(res.totalCount);
        });
    }
    $scope.$watchGroup(['pager.index', 'pager.size'], function () { getSamples(true); });
    function countSampleTranscript(sample) {
        sample.CorrectionPendingCount = 0;
        sample.CorrectedCount = 0;
        sample.MasterCount = 0;
        sample.TrashedCount = 0;
        sample.transcripts.forEach(function (t) {
            if (t.Status === transcriptStatuses.CorrectionPending)
                sample.CorrectionPendingCount++;
            else if (t.Status === transcriptStatuses.Corrected) {
                if (t.IsMaster)
                    sample.MasterCount++;
                else
                    sample.CorrectedCount++;
            } else if (t.Status === transcriptStatuses.Trashed)
                sample.TrashedCount++;
        });
    }
    function updateSampleStatus(sample) {
        sample.Status = sample.Queued == null ? "not ordered" : (sample.Queued > sample.Transcribed ? "queued" : "done");
        if (sample.MasterCount > 0)
            sample.Status = "completed";
        else if (sample.CorrectionPendingCount + sample.CorrectedCount > 0)
            sample.Status = "in review";
    }
    function getTranscripts(sample) {
        Service('GetTranscripts', { songId: sample.Id }).then(function (transcripts) {
            transcripts.forEach(function (transcript) { transcript.EditStart = transcript.EditStart ? moment(transcript.EditStart) : null });
            sample.transcripts = transcripts;
        });
    }
    $scope.toggleTranscripts = function (sample) {
        if (sample.transcripts)
            sample.transcripts = null;
        else
            getTranscripts(sample);
    };
    Service('GetDistinctFilters').then(function (res) {
        $scope.products = commonFilters.concat();
        res.Products.forEach(function (product) {
            $scope.products.push({ label: product, value: product });
        });

        $scope.categories = commonFilters.concat();
        res.Categories.forEach(function (category) {
            $scope.categories.push({ label: category, value: category });
        });

        $scope.companies = commonFilters.concat();
        res.Companies.forEach(function (company) {
            $scope.companies.push({ label: company, value: company });
        });

        $scope.brands = commonFilters.concat();
        res.Brands.forEach(function (brand) {
            $scope.brands.push({ label: brand, value: brand });
        });

        $scope.distinctFiltersLoaded = true;
        getSamples();
    });

    $scope.selected = transcriptSelection.current;
    var lastSelection = {
        index: -1,
        selected: null
    };
    $scope.selectionClicked = function (event, index) {
        var songId = $scope.samples[index].Id;
        var isSelected = transcriptSelection.isSelected(songId);
        if (!event.shiftKey) {
            isSelected = transcriptSelection.set(songId, !isSelected);
            lastSelection.selected = isSelected;
        }
        if (event.shiftKey && lastSelection.index != -1) {
            event.preventDefault();
            for (var i = Math.min(index, lastSelection.index) ; i <= Math.max(index, lastSelection.index) ; ++i)
                transcriptSelection.set($scope.samples[i].Id, lastSelection.selected);
        }
        lastSelection.index = index;
    };
    $scope.countSelected = function () {
        return transcriptSelection.count();
    };
    function getSelectedSampleIds() {
        return transcriptSelection.getIds();
    }
    $scope.orderTranscripts = function () {
        var songIds = getSelectedSampleIds();
        OrderTranscriptsWindow.confirm(songIds.length).then(function (answer) {
            if (answer.confirmed && answer.transcriptCount > 0)
                Service('OrderTranscripts', {
                    songIds: songIds,
                    quantity: answer.transcriptCount,
                    priority: answer.priority
                }).then(function () {
                    samples.forEach(function (sample) {
                        sample.Queued = (sample.Queued || 0) + answer.transcriptCount;
                        updateSampleStatus(sample);
                    });
                });
        })
    };
    $scope.priority = 50; // Normal
    $scope.priorities = [0, 50, 100];
    $scope.pickPriority = function (p) { $scope.priority = p; }
    $scope.setPriority = function () {
        Service('SetTranscriptQueuePriority', {
            songIds: getSelectedSampleIds(),
            priority: $scope.priority
        }).then(getSamples);
    }

    $scope.unqueue = function () {
        Service('ClearTranscriptQueue', {
            songIds: getSelectedSampleIds()
        }).then(getSamples);
    }

    $scope.editRemindEmail = function () {
        Service('GetTranscribersHavingQueue').then(function (userNames) {
            var recipients = userNames.length > 0 ? userNames.join(', ') : 'nobody';
            confirmPopup.open("Notify that samples are waiting to be transcribed", null, "To: " + recipients).then(function (template) {
                Service('RemindAwaitingTranscripts');
            });
        });
    };

    var player = null;
    $scope.isPaused = true;
    function positionChanged(position) {
        $scope.$apply(function () {
            $scope.position = position;
        });
    }
    function stop() {
        if (player && player.playState == 1)
            player.stop();
        $scope.isPaused = true;
    }
    $scope.pause = function () {
        player.togglePause();
        $scope.isPaused = player.paused;
    };
    $scope.seeked = function (pos) {
        var position = $scope.sample.Duration * pos;
        player.setPosition(position);
    };

    $scope.editTranscript = function (sample, transcript) {
        $scope.sample = sample;
        $scope.transcript = transcript;
        sample.fullText = transcript.Text;

        player = soundManager.createSound({
            id: 'aSound'
        });
        var options = {
            url: transcript.FilePath,
            loops: 10,
            whileplaying: function () {
                positionChanged(player.position);
            }
        };
        player.play(options);
        $scope.isPaused = false;
    };
    $scope.saveTranscript = function (asMaster, status) {
        Service('SaveFullText', { transcriptId: $scope.transcript.Id, fullText: $scope.sample.fullText, asMaster: asMaster, status: status });
        $scope.transcript.Text = $scope.sample.fullText;
        $scope.transcript.Status = status;
        if (asMaster)
            $scope.sample.transcripts.forEach(function (transcript) { transcript.IsMaster = transcript === $scope.transcript; });
        countSampleTranscript($scope.sample);
        updateSampleStatus($scope.sample);
        $scope.cancelTranscript();
    };
    $scope.cancelTranscript = function () {
        stop();
        $scope.sample = null;
    };

    $scope.$on('$destroy', function () {
        stop();
        player.destruct();
    });
}])
.controller('wordCutCtrl', ['$scope', 'Service', 'soundmanager', 'PositionEvents', '$timeout', function ($scope, Service, soundmanager, PositionEvents, $timeout) {
    var player = null;
    var positionEvents = new PositionEvents();
    soundmanager.then(function () {
        player = soundManager.createSound({
            id: 'aSound'
        });
        positionEvents.player = player;
        pickNext();
    });
    function pickNext() {
        Service('PickWordToCut').then(function (wordToCut) {
            $scope.wordToCut = wordToCut;
            $scope.editing.controlStart = true;
            $scope.editing.start = wordToCut.PartStart;
            $scope.editing.end = wordToCut.PartEnd;
            play();
        });
    }
    function play() {
        var options = {
            url: $scope.wordToCut.FilePath,
            from: $scope.editing.start,
            whileplaying: function () {
                positionChanged(player.position);
            }
        };

        // Safe loop back - position event may never happen if the looping position matches the sample end
        var isLastPart = $scope.editing.end === $scope.wordToCut.Duration;
        function doLoop() {
            positionChanged($scope.editing.end);
            delayedPlay();
        }
        if (isLastPart)
            options.onfinish = doLoop;
        else
            positionEvents.add($scope.editing.end, doLoop);

        player.play(options);
        $scope.isPaused = false;
    }
    function delayedPlay() {
        stop();
        $timeout(play, 1000);
    }
    function positionChanged(pos) {
        $scope.$apply(function () {
            $scope.position = pos;
        });
    }
    function stop() {
        positionEvents.clearAll();
        if (player && player.playState == 1)
            player.stop();
        $scope.isPaused = true;
    }
    $scope.pause = function () {
        player.togglePause();
        $scope.isPaused = player.paused;
    };
    $scope.seeked = function (pos) {
        var position = $scope.wordToCut.Duration * pos;
        player.setPosition(position);
    };

    $scope.editing = {
        controlStart: true,
        start: 0,
        end: 0
    };

    function keyDowned(ev) {
        ev = ev || window.event;
        console.log(ev.keyCode);
    }
    document.addEventListener('keydown', keyDowned);
    $scope.$destroy(function () {
        document.removeEventListener('keydown', keyDowned, false);
        stop();
        player.destruct();
    });
}])
.controller('taggerLabCtrl', ['$scope', 'Service', 'soundmanager', 'Spreadsheet', function ($scope, Service, soundmanager, Spreadsheet) {
    var player = null;
    $scope.isPaused = true;
    $scope.itemTypes = [
    { id: 0, name: 'Exclusive' },
    { id: 1, name: 'Co Brand' },
    { id: 2, name: 'Co-op Brand' },
    { id: 3, name: 'Other Brand' },
    { id: 4, name: 'Sponsorship - Title' },
    { id: 5, name: 'Sponsorship - Secondary' },
    { id: 6, name: 'Station Joint Promo - Title' },
    { id: 7, name: 'Station Joint Promo - Secondary' }
    ];
    $scope.adTypes = [
    { id: 0, name: 'Spot: Advertiser' },
    { id: 1, name: 'Spot: Station/Advertiser Joint Promotion' },
    { id: 2, name: 'Ad lib/Live Read: Advertiser' },
    { id: 3, name: 'Ad lib/Live Read: Station/Advertiser Joint Promotion' },
    { id: 4, name: 'Promo: Corporate Family' },
    { id: 5, name: 'Promo: Station' },
    { id: 6, name: 'Station ID' },
    { id: 7, name: 'Sponsored Program Promotion' },
    { id: 8, name: 'PSA' }
    ];
    $scope.musicTypes = [
    { id: 0, name: 'None' },
    { id: 1, name: 'Production Music' },
    { id: 2, name: 'Jingle' },
    { id: 3, name: 'Song' },
    ];
    soundmanager.then(function () {
        player = soundManager.createSound({
            id: 'aSound'
        });
        pickNext();
    });
    function pickNext() {
        Service('PickSongToTag').then(function (songToTag) {
            var a = /\/Date\((\d*)\)\//.exec(songToTag.AdTagging.OneAd.ExpirationDate);
            if (a != null)
                songToTag.AdTagging.OneAd.ExpirationDate = new Date(+a[1]);
            $scope.songToTag = songToTag;

            var options = {
                url: songToTag.FilePath,
                loops: 10,
                whileplaying: function () {
                    $scope.$apply(function () {
                        $scope.position = player.position;
                    });
                },
                from: 0
            };
            player.play(options);
            $scope.isPaused = false;
        });
    }
    $scope.openDatePicker = function ($event) {
        $event.preventDefault();
        $event.stopPropagation();

        $scope.opened = true;
    };
    function stop() {
        if (player && player.playState == 1)
            player.stop();
        $scope.isPaused = true;
    }
    $scope.pause = function () {
        player.togglePause();
        $scope.isPaused = player.paused;
    };
    $scope.seeked = function (pos) {
        var position = $scope.songToTag.AdTagging.Duration * 1000 * pos;
        player.setPosition(position);
    };
    $scope.addFact = function () {
        $scope.songToTag.AdTagging.OneAd.AdFacts.push({});
    };
    $scope.getItems = function (setName, search) {
        if (search.length < 2)
            return [];
        return Service('SuggestLabel', { setName: setName, search: search }, { backgroundLoad: true });
    };
    $scope.save = function (status) {
        $scope.songToTag.AdTagging.Status = status;
        Service('SaveAdTagging', { adTagging: $scope.songToTag.AdTagging }).then(pickNext);
    };
    $scope.export = function () {
        Spreadsheet('taggerLabExport/' + $scope.user.id);
    };
    $scope.$on('$destroy', function () {
        stop();
        player.destruct();
    });
}])
.controller('matcherCtrl', ['$scope', 'Service', 'soundmanager', '$audioPlayer', function ($scope, Service, soundmanager, $audioPlayer) {
    var player1;
    var player2;
    $scope.comment = { text: '' };
    $scope.picked = { date: null };
    $scope.playing = { index: 1 };

    $scope.pickOldest = function () {
        $scope.picked.date = null;
        $scope.pickNext();
    };
    $scope.pickByDate = function () {
        $scope.picked.date = moment().format('YYYY-MM-DD');
        $scope.pickNext();
    };
    $scope.dateChanged = function () {
        window.setTimeout(function () {
            $scope.picked.date = $scope.picked.date ? moment($scope.picked.date).format('YYYY-MM-DD') : null;
            $scope.pickNext();
        });
    };

    soundmanager.then(function () {
        player1 = new $audioPlayer('aSound', $scope);
        player2 = new $audioPlayer('bSound', $scope);
        $scope.player1 = player1;
        $scope.player2 = player2;
        pickNext();
    });
    function pickNext() {
        Service('PickNextToMatch', { date: $scope.picked.date }).then(gotNext);
    }
    function gotNext(songToMatch) {
        stop(1);
        stop(2);

        $scope.comment.text = '';
        $scope.comment.analyzeStart = moment();
        $scope.songToMatch = songToMatch;
        $scope.selectedMatch = songToMatch.GroupedMatches[0];

        if (songToMatch) {
            receiveMatch(songToMatch);
            var options1 = {
                url: songToMatch.SourceMp3Path,
                loops: 10
            };
            player1.seek(0);
            player1.play(options1);
            $scope.playing.index = 1;
        }
    }
    function receiveMatch(songToMatch) {
        // Get overall T0 and maxEndTime
        songToMatch.T0 = 0;
        songToMatch.maxEndTime = 0;
        songToMatch.GroupedMatches.forEach(function (groupedMatch) {
            groupedMatch.T0 = Math.min(0, _.min(groupedMatch.OffsetGroups, 'Offset').Offset);
            songToMatch.T0 = Math.min(songToMatch.T0, groupedMatch.T0);

            var maxEndTime = _.max(_.map(groupedMatch.OffsetGroups, function (offsetGroup) { return offsetGroup.Offset + groupedMatch.Duration; }));
            songToMatch.maxEndTime = Math.max(songToMatch.maxEndTime, maxEndTime);
        });

        songToMatch.GroupedMatches.forEach(function (groupedMatch) {
            groupedMatch.TimeFrame = Math.max(songToMatch.Duration, songToMatch.maxEndTime) - songToMatch.T0;

            groupedMatch.OffsetGroups.forEach(function (offsetGroup) {
                offsetGroup.CommonDuration = offsetGroup.Chunks.reduce(function (total, chunk) {
                    return total + (chunk.SourceEnd - chunk.SourceStart);
                }, 0);
            });
        });
    }
    function play(match) {
        $scope.pause(1);
        var options2 = {
            url: match.TargetMp3Path,
            loops: 10,
            from: 0
        };
        stop(2);
        player2.seek(0);
        player2.play(options2);
        $scope.playing.index = 2;
    }
    function stop(index) {
        if (index === 1) {
            player1.stop();
        } else {
            if (player2)
                player2.stop();
        }
    }
    $scope.pause = function (index) {
        if (index === 1 && !player1.player.paused) {
            player1.pause();
        } else if (index === 2 && !player2.player.paused) {
            player2.pause();
        }
    };
    $scope.togglePause = function (index) {
        if (index === 1) {
            player1.togglePause();
            if (!player1.player.paused) {
                $scope.pause(2);
                $scope.playing.index = 1;
            }
        } else if (index === 2 && player2) {
            player2.togglePause();
            if (!player2.player.paused) {
                $scope.pause(1);
                $scope.playing.index = 2;
            }
        }
    };
    $scope.seeked1 = function (pos) {
        $scope.pause(2);
        var position = $scope.songToMatch.Duration * pos;
        player1.seek(position);
        $scope.playing.index = 1;
    };
    $scope.seeked2 = function (pos) {
        $scope.pause(1);
        var position = $scope.selectedMatch.Duration * pos;
        player2.seek(position);
        $scope.playing.index = 2;
    };
    $scope.selectAndSeek = function (match, pos) {
        $scope.pause(1);
        $scope.selectedMatch = match;
        play(match);
        $scope.seeked2(pos);
    };
    $scope.commentThenPickNext = function () {
        Service('CommentAndPickNextToMatch', {
            pksid: $scope.songToMatch.SourcePksid,
            masterPksid: $scope.selectedMatch.TargetSample,
            comment: $scope.comment.text,
            analyzeDuration: moment().diff($scope.comment.analyzeStart, 'seconds'),
            date: $scope.picked.date
        }).then(gotNext);
    };
    $scope.pickNext = pickNext;
    $scope.pickThis = function ($event, match) {
        $event.stopPropagation();
        $scope.pickThisPksid(match.TargetSample);
    };
    $scope.pickThisPksid = function (pksid) {
        Service('PickThisToMatch', { pksid: pksid }).then(gotNext);
    };
    $scope.$on('$destroy', function () {
        stop(1);
        stop(2);
        player1.destroy();
        player2.destroy();
    });
}])
.controller('playoutMapCtrl', ['$scope', 'Service', 'fs', function ($scope, Service, fs) {
    //Service('GetCountries').then(function (countries) {
    //	$scope.countries = countries.map(function (c) {
    //		return {
    //			code: c[0],
    //			name: c[1]
    //		};
    //	});
    //});
    //$scope.setCountry = function (country) { $scope.country = country; };
    //$scope.importCountry = function () {
    //	Service('ImportCountryCities', { countryCode: $scope.country.code }).then(getCities);
    //};
    function getCities() {
        Service('GetCities', { countryCode: null }).then(function (cities) {
            $scope.cities = cities;

            $scope.markers = {};
            cities.forEach(function (city) {
                $scope.markers[city.osm_id] = {
                    lat: city.lat,
                    lng: city.lng,
                    focus: true,
                    draggable: false,
                    opacity: 0.5,
                    city: city
                };
            });

            $scope.bounds = {
                northEast: {
                    lat: Math.max.apply(this, cities.map(function (city) { return city.lat; })),
                    lng: Math.max.apply(this, cities.map(function (city) { return city.lng; }))
                }, southWest: {
                    lat: Math.min.apply(this, cities.map(function (city) { return city.lat; })),
                    lng: Math.min.apply(this, cities.map(function (city) { return city.lng; }))
                }
            };

            loadChannels();
        });
    }
    getCities();

    function loadChannels() {
        Service('GetChannelsAndCityCount').then(function (channels) {
            $scope.channels = channels;

            var channelGrid = new fs.Grid(channels);
            new fs.Column(channelGrid, 'Channel Name', 'Name', new fs.FilterSubstring());
            new fs.Column(channelGrid, 'City', 'City', new fs.FilterFromData(channels, 'City'));
            new fs.Column(channelGrid, 'Country', 'Country', new fs.FilterFromData(channels, 'Country'));
            new fs.Column(channelGrid, 'Media', 'MediaType', new fs.FilterFromData(channels, 'MediaType'));
            new fs.Column(channelGrid, 'Positions', 'CityCount');
            $scope.channelGrid = channelGrid;

            $scope.orderByChannelGrid = function (channel) {
                return $scope.channelGrid.orderKey(channel);
            };

            $scope.setChannel(_.first(channels));
        });
    }

    $scope.setChannel = function (channel) {
        $scope.currentChannel = channel;
        $scope.channelCities = []; // while loading
        if (channel)
            Service('GetChannelCities', { channelId: channel.Id }).then(function (cityIds) {
                $scope.cities.forEach(function (city) {
                    unselectCity(city);
                    if (cityIds.some(function (id) { return id === city.osm_id }))
                        selectCity(city);
                });

                var cityGrid = new fs.Grid($scope.channelCities);
                new fs.Column(cityGrid, 'Name', 'name');
                new fs.Column(cityGrid, 'Kind', 'kind');
                new fs.Column(cityGrid, 'Population', 'population');
                new fs.Column(cityGrid, 'Latitude', 'lat');
                new fs.Column(cityGrid, 'Longitude', 'lng');
                $scope.cityGrid = cityGrid;

                $scope.orderByCityGrid = function (city) {
                    return $scope.cityGrid.orderKey(city);
                };
            });
    };
    function unselectCity(city) {
        $scope.markers[city.osm_id].opacity = 0.4;
        var pos = getCityPos(city);
        if (pos !== -1)
            $scope.channelCities.splice(pos, 1);
        updateCityCount();
    }
    function selectCity(city) {
        $scope.markers[city.osm_id].opacity = 1;
        $scope.channelCities.push(city);
        updateCityCount();
    }
    function updateCityCount() {
        $scope.currentChannel.CityCount = $scope.channelCities.length;
    }
    function getCityPos(city) {
        var pos = -1;
        $scope.channelCities.some(function (c, index) {
            if (c.osm_id === city.osm_id) {
                pos = index;
                return true;
            }
        });
        return pos;
    }

    function addCity(city) {
        return Service('AddChannelCity', { channelId: $scope.currentChannel.Id, cityId: city.osm_id }, { backgroundLoad: true });
    }
    function removeCity(city) {
        return Service('RemoveChannelCity', { channelId: $scope.currentChannel.Id, cityId: city.osm_id }, { backgroundLoad: true });
    }

    //$scope.bounds = { northEast: { lat: 46.5, lng: 19.4 }, southWest: { lat: 42.6, lng: 13.5 } };
    $scope.$on('leafletDirectiveMarker.click', function (event, args) {
        var city = args.model.city;
        var pos = getCityPos(city) !== -1;
        console.log(city, pos);
        var selected = pos;
        if (selected) {
            unselectCity(city);
            removeCity(city);
        } else {
            selectCity(city);
            addCity(city);
        }
    });
}])
.factory('OrderTranscriptsWindow', ['$modal', function ($modal) {
    return {
        confirm: function (sampleCount) {
            return $modal.open({
                animation: false,
                templateUrl: 'orderTranscripts.html',
                controller: 'OrderTranscriptsCtrl',
                size: 'sm',
                resolve: {
                    sampleCount: function () {
                        return sampleCount;
                    }
                }
            }).result;
        }
    }
}])
.controller('OrderTranscriptsCtrl', ['$scope', '$modalInstance', 'sampleCount', function ($scope, $modalInstance, sampleCount) {
    $scope.sampleCount = sampleCount;
    $scope.transcriptCount = 1;
    $scope.priorities = [100, 50, 0];
    $scope.priority = $scope.priorities[1]; // Normal
    $scope.setPriority = function (p) {
        $scope.priority = p;
    }
    $scope.ok = function () {
        $modalInstance.close({ confirmed: true, transcriptCount: $scope.transcriptCount, priority: $scope.priority });
    };
    $scope.cancel = function () {
        $modalInstance.close({ confirmed: false });
    };
}])
.controller('mailingCtrl', ['$scope', 'Service', 'debounce', function ($scope, Service, debounce) {
    $scope.recipientEmails = [];
    $scope.htmlContent = '';
    var claimList = [];
    Service('GetAllClaims').then(function (claims) {
        claimList = claims;
        // convert from a list of claim (name/value) pairs into a map[name][value]
        $scope.claims = claims.reduce(function (map, claim) {
            map[claim.Name] = map[claim.Name] || {};
            map[claim.Name][claim.Value] = false;
            return map;
        }, {});
    });
    $scope.updateRecipientCount = debounce(500, function () {
        Service('GetRecipientEmails', { claims: getSelectedClaims() }).then(function (emails) { $scope.recipientEmails = emails; });
    });
    function getSelectedClaims() {
        return claimList.reduce(function (list, c) {
            if ($scope.claims[c.Name][c.Value])
                list.push(c);
            return list;
        }, []);
    }
    $scope.sendToMe = function () {
        $scope.mailSent = undefined;
        Service('SendMailingTest', {
            fromDisplayName: $scope.fromDisplayName,
            subject: $scope.subject,
            html: $scope.htmlContent
        }).then(function (res) {
            $scope.mailSent = res;
        });
    };
    $scope.sendMailing = function () {
        if (confirm("Send the mailing to " + $scope.recipientCount + " people?")) {
            Service('SendMailing', {
                fromDisplayName: $scope.fromDisplayName,
                subject: $scope.subject,
                html: $scope.htmlContent,
                claims: getSelectedClaims()
            }).then(function (res) {
                $scope.mailSent = res > 0;
            });
        }
    };
}])
.factory('Labels', function () {
    var labels = {
        criteria: {
            0: {
                Name: 'Media',
                ParentIds: []
            },
            1: {
                Name: 'Industry',
                ParentIds: []
            },
            2: {
                Name: 'Advertiser',
                ParentIds: [1]
            },
            3: {
                Name: 'Brand',
                ParentIds: [1, 2] // Industry, Advertiser
            },
            4: {
                Name: 'Channel',
                ParentIds: [0], // Media
            },
            5: {
                Name: 'Day of Week',
                ParentIds: []
            },
            6: {
                Name: 'Day Part',
                ParentIds: []
            },
            7: {
                Name: 'Category',
                ParentIds: [1] // Industry
            },
            8: {
                Name: 'Campaign',
                ParentIds: [1, 2, 3] // Industry, Advertiser, Brand
            },
            9: {
                Name: 'Sample Title',
                ParentIds: [1, 2, 3, 8] // Industry, Advertiser, Brand, Campagn
            }
        },
        values: {
            0: { Id: 0, Name: 'Spot Count' },
            1: { Id: 1, Name: 'Air Time' },
            2: { Id: 2, Name: 'Average Spot Length' },
            //3: { Id: 3, Name: 'Media Mix' }, // Percentage
            4: { Id: 4, Name: 'Expense' },
        }
    };
    for (var criteriaId in labels.criteria) {
        var criteria = labels.criteria[criteriaId];
        criteria.Id = parseInt(criteriaId, 10);
        criteria.Parents = criteria.ParentIds.map(function (parentCriteriaId) { return labels.criteria[parentCriteriaId]; });
    }
    return labels;
})
.controller('reportingCtrl', ['$scope', 'Service', '$modal', 'Labels', 'LocalStorage', function ($scope, Service, $modal, Labels, LocalStorage) {
    $scope.industrySelect = { all: false, list: [] };
    LocalStorage.cacheOrGenerate('GetDataTimeRange', null, function () {
        return Service('GetDataTimeRange');
    }).then(function (range) {
        LocalStorage.cacheOrGenerate('GetAllIndustries', null, function () {
            return Service('GetAllIndustries');
        }).then(function (industries) {
            $scope.industries = industries;

            var selectedIndustries = LocalStorage.getJson('selectedIndustries') || $scope.industrySelect;
            $scope.selectedIndustriesString = getSelectedIndustriesString();
            _.forEach(industries, function (industry) {
                industry.Selected = _.some(selectedIndustries.list, function (industryId) { return industry.Id === industryId });
            });
            $scope.industrySelect.all = selectedIndustries.all;
        });

        $scope.common = {
            range: {
                start: moment(range.Start),
                end: moment(range.End),
                kind: 'week', // By default select week ranges
                interpolation: 0, // No interpolation
            }
        };
        $scope.tab = [false, true];

        function generateMonth() {
            var months = [];
            var date = moment($scope.common.range.start).startOf('month');
            while (!date.isSame($scope.common.range.end, 'month')) {
                months.push({
                    Start: moment(date),
                    End: moment(date).add(1, 'M'),
                    Name: moment(date).format('YYYY MMMM')
                });
                date.add(1, 'M');
            }
            $scope.common.months = months;
        }
        function generateWeeks() {
            var weeks = [];
            var date = moment($scope.common.range.start).startOf('isoWeek');
            while (!date.isSame($scope.common.range.end, 'isoWeek')) {
                weeks.push({
                    Start: moment(date),
                    End: moment(date).add(1, 'w'),
                    Name: moment(date).format('GGGG [Week] W')
                });
                date.add(1, 'w');
            }
            $scope.common.weeks = weeks;
        }

        generateMonth();
        generateWeeks();

        function rangeKindChanged(kind) {
            // TODO: control weeks.length
            // TODO: when former values exist in first and second, try to match the dates in the other list
            if (kind == 'week') {
                $scope.common.range.first = $scope.common.weeks[$scope.common.weeks.length - 1];
                $scope.common.range.second = $scope.common.weeks[$scope.common.weeks.length - 2];
            } else {
                $scope.common.range.first = $scope.common.months[$scope.common.months.length - 1];
                $scope.common.range.second = $scope.common.months[$scope.common.months.length - 2];
            }
        }
        $scope.$watch('common.range.kind', rangeKindChanged);
    });

    Service('GetPredefinedReporters').then(function (reporters) {
        $scope.PredefinedReporters = reporters;
    });

    function loadMyReporters() {
        $scope.MyReporters = LocalStorage.getJson('my-reporters') || [];
    }
    loadMyReporters();

    $scope.criteria = [];
    $scope.hasEnoughtCriteriaForPivot = false;

    Object.keys(Labels.criteria).forEach(function (criteriumId) { $scope.criteria.push(Labels.criteria[criteriumId]); });
    $scope.values = Labels.values;
    $scope.values[0].Selected = true; // Select the Spot count value by default
    var criteriaOrder = 0;
    $scope.criteriumToggled = function (criterium) {
        if (criterium.Selected) {
            criterium.Parents.forEach(function (parentCriterium) {
                if (!parentCriterium.Selected) {
                    parentCriterium.Selected = true;
                    parentCriterium.Disabled = true;
                    parentCriterium.GroupBy = false;
                }
                parentCriterium.Order = criteriaOrder++;
            });
            criterium.GroupBy = true;
            criterium.Order = criteriaOrder++;
        } else {
            criterium.Parents.forEach(function (parentCriterium) {
                parentCriterium.Disabled = false;
            });
        }
        $scope.hasEnoughtCriteriaForPivot = enoughCriteriaForPivot();
    };
    $scope.hideCriteria = function (criteriaId) {
        var criterium = $scope.criteria[criteriaId];
        criterium.Selected = false;
        $scope.criteriumToggled(criterium);
        $scope.generateReport();
    };
    function enoughCriteriaForPivot() {
        return _.where($scope.criteria, { 'Selected': true }).length >= 2;
    }
    $scope.isSingleCriteria = function () {
        return $scope.reporter.Criteria.length == 1;
    };
    $scope.canGenerate = function () {
        return _.filter($scope.values, { 'Selected': true }).length > 0 &&
        _.filter($scope.criteria, { 'Selected': true }).length > 0;
    };
    $scope.reporter = {
        Range: { Start: null, End: null },
        Range2: { Start: null, End: null },
        Interpolation: 0,
        Industries: null,
        Companies: null,
        Brands: null,
        Channels: null,
        Criteria: [],
        Pivot: false,
        Values: [],
        OrderingValue: 0,
        OrderDescending: false
    };
    $scope.newReporterName = '';
    $scope.saveCurrentReporter = function () {
        $scope.MyReporters.push({ Name: $scope.newReporterName, Reporter: $scope.reporter });
        LocalStorage.setJson('my-reporters', $scope.MyReporters);
        $scope.newReporterName = '';
        loadMyReporters();
    };
    $scope.deleteReporter = function (index) {
        $scope.MyReporters.splice(index, 1);
        LocalStorage.setJson('my-reporters', $scope.MyReporters);
        loadMyReporters();
    };

    $scope.generateReport = function () {
        $scope.reporter.Criteria = [];
        var selectedCriteria = _.sortBy(_.filter($scope.criteria, 'Selected'), 'Order');
        var groupingCriteria = _.filter(selectedCriteria, function (criteria) {
            return criteria.GroupBy || // Marked as grouping
            criteria.Id == _.last(selectedCriteria).Id; // Last criteria is implicitely grouping
        });
        _.forEach(groupingCriteria, function (criteria) {
            var c = {
                Criteria: criteria.Id,
                Informative: _.pluck(_.filter(criteria.Parents, function (parent) { return parent.Selected && !parent.GroupBy; }), 'Id'),
                OrderDescending: criteria.OrderDescending
            };
            $scope.reporter.Criteria.push(c);
        });

        $scope.reporter.Values = _.pluck(_.filter($scope.values, 'Selected'), 'Id');

        $scope.reporter.Industries = [];
        if (!$scope.industrySelect.all) // Sending no industry means no industry filter = all industries
            for (var i in $scope.industries)
                if ($scope.industries[i].Selected)
                    $scope.reporter.Industries.push({ Id: $scope.industries[i].Id, Name: $scope.industries[i].Name });

        if (!$scope.hasEnoughtCriteriaForPivot || $scope.common.range.interpolation !== 0)
            $scope.reporter.Pivot = false;

        $scope.hiddenColumns = [];

        // Normalize date ranges
        $scope.reporter.Interpolation = $scope.common.range.interpolation;
        if ($scope.common.range.interpolation == 2 && $scope.common.range.kind == 'month')
            $scope.reporter.Interpolation = 3;

        $scope.reporter.Range = $scope.common.range.first;
        $scope.reporter.Range2 = $scope.common.range.second;
        if ($scope.reporter.Interpolation === 0)
            $scope.reporter.Range2 = {};
        else if ($scope.reporter.Range.Start.isAfter($scope.reporter.Range2.Start)) { // Reorder ranges
            var r1 = $scope.reporter.Range;
            $scope.reporter.Range = $scope.reporter.Range2;
            $scope.reporter.Range2 = r1;
        }
        getReport($scope.reporter);
    };
    function getReport(reporter) {
        return LocalStorage.cacheOrGenerate('report', reporter, function () {
            return Service('GetReport', { reporter: reporter }).then(gotReport);
        });
    }
    function convertPivotToDictionary(rows) {
        for (var level0 = 0; level0 < rows.length; ++level0) {
            if (rows[level0].PivotValues) {
                rows[level0].PivotValues = rows[level0].PivotValues.reduce(function (dic, pivotValue) {
                    dic[pivotValue.Index] = pivotValue.Values;
                    return dic;
                }, {});
                if (rows[level0].SubRows)
                    convertPivotToDictionary(rows[level0].SubRows);
            }
        }
    };
    function gotReport(report) {
        if (report.Reporter.Pivot || report.Reporter.Interpolation != 0)
            convertPivotToDictionary(report.Rows);
        $scope.report = report;

        $scope.rowCriteriaLists = [
        rowCriteriaList(0),
        rowCriteriaList(1),
        rowCriteriaList(2),
        rowCriteriaList(3)
        ];
    }
    $scope.remindList = function (rowAncestry) {
        var list = [];
        rowAncestry.forEach(function (row) {
            row.InformativeValues.forEach(function (info) { list.push(info); });
            list.push(row.Title);
        });
        return list;
    };
    function rowCriteriaList(level) {
        var criteria = _.slice($scope.reporter.Criteria, level + 1);
        if ($scope.report.Reporter.Pivot)
            criteria = _.initial(criteria);
        var flat = [];
        criteria.forEach(function (crit) {
            crit.Informative.forEach(function (info) { flat.push($scope.criteria[info].Name); });
            flat.push($scope.criteria[crit.Criteria].Name);
        });
        return flat;
    }
    $scope.hiddenColumns = [];
    $scope.hideColumn = function ($event) {
        $scope.hiddenColumns.push('hidden-col' + getChildNumber($event.target.parentNode));
    };
    function getChildNumber(node) {
        return Array.prototype.indexOf.call(_.filter(node.parentNode.childNodes, { tagName: node.tagName }), node);
    }
    $scope.rangeKind = 'week';
    $scope.setReporter = function (reporter) {
        _.forEach($scope.criteria, function (criterium) {
            criterium.Selected = false;
            criterium.Disabled = false;
            criterium.OrderDescending = null;
        });
        criteriaOrder = 0;
        function getCriterium(id) { return _.find($scope.criteria, { Id: id }); }
        _.forEach(reporter.Criteria, function (criterium) {
            _.forEach(criterium.Informative, function (informative) {
                var c = getCriterium(informative);
                c.Selected = true;
                c.GroupBy = false;
                c.Disabled = true;
                c.Order = criteriaOrder++;
            });
            var c = getCriterium(criterium.Criteria);
            c.Selected = true;
            c.GroupBy = true;
            c.Order = criteriaOrder++;
        });
        $scope.reporter.Pivot = reporter.Pivot;
        _.forEach($scope.values, function (value) { value.Selected = false; });
        _.forEach(reporter.Values, function (valueId) {
            var value = _.find($scope.values, { Id: valueId });
            value.Selected = true;
        });

        if (reporter.Pivot) // Pivot cannot be combined with interpolation
            $scope.common.range.interpolation = 0;

        $scope.reporter.OrderingValue = reporter.OrderingValue;
        $scope.reporter.OrderDescending = reporter.OrderDescending;
        $scope.generateReport();
    }

    function getSelectedIndustriesString() {
        if (!$scope.industrySelect)
            return '';
        if ($scope.industrySelect.all)
            return "All Industries";

        var s = [];
        for (var i in $scope.industries)
            if ($scope.industries[i].Selected)
                s.push($scope.industries[i].Name);

        if (s.length == 0)
            return "No Industry selected";

        return s.join(', ');
    }
    $scope.selectIndustries = function () {
        var modalInstance = $modal.open({
            animation: false,
            templateUrl: 'tplIndustrySelector.html',
            controller: function ($scope, $modalInstance, industries, industrySelect) {
                $scope.industries = industries;
                $scope.industrySelect = industrySelect;
                $scope.selectAll = function () {
                    industrySelect.all = true;
                };
                $scope.unselectAll = function () {
                    industrySelect.all = false;
                    for (var i in industries)
                        industries[i].Selected = false;
                };
                $scope.ok = function () {
                    LocalStorage.setJson('selectedIndustries', { all: industrySelect.all, list: _.pluck(_.where(industries, { 'Selected': true }), 'Id') });
                    $scope.selectedIndustriesString = getSelectedIndustriesString();
                    $modalInstance.close();
                };
            },
            resolve: {
                industries: function () {
                    return $scope.industries;
                },
                industrySelect: function () {
                    return $scope.industrySelect;
                }
            }
        });
    };
    $scope.orderByCriteria = function (criteriaId, orderDescending, generateImmediately) {
        var cancelOrdering = $scope.criteria[criteriaId].OrderDescending === orderDescending;
        $scope.criteria[criteriaId].OrderDescending = cancelOrdering ? null : orderDescending;
        if (generateImmediately)
            $scope.generateReport();
    };
    $scope.orderByValue = function (valueId, orderDescending, generateImmediately) {
        $scope.reporter.OrderingValue = valueId;
        $scope.reporter.OrderDescending = orderDescending;
        $scope.values[valueId].Selected = true;
        if (generateImmediately)
            $scope.generateReport();
    };
    $scope.drillDownCriteria = function (level, rowTitle) {
        // Get the criteria that has been selected
        var levelCriteria = $scope.reporter.Criteria[level];
        var criteriaId = levelCriteria.Criteria;

        // Other criteria is used as the pivot value
        var otherCriteria = level > 0 ? $scope.reporter.Criteria[level - 1] : null;

        // SubCriteria are the remaining criteria that become the main criteria
        var subCriteria = levelCriteria.Informative;

        showDrillDownModal(levelCriteria, otherCriteria, subCriteria, criteriaId, rowTitle, $scope.reporter);
    };
    $scope.drillDownInformative = function (level, index, rowTitle) {
        // Get the criteria that has been selected: it is an informative value
        var levelCriteria = $scope.reporter.Criteria[level];
        var criteriaId = levelCriteria.Informative[index];

        // Other criteria is used as the pivot value
        var otherCriteria = level > 0 ? $scope.reporter.Criteria[level - 1] : null;

        // SubCriteria are the remaining criteria that become the main criteria
        var subCriteria = _.rest(levelCriteria.Informative, index + 1);

        showDrillDownModal(levelCriteria, otherCriteria, subCriteria, criteriaId, rowTitle, $scope.reporter);
    };
    function showDrillDownModal(levelCriteria, otherCriteria, subCriteria, criteriaId, title, currentReporter) {
        var criteria = Labels.criteria[criteriaId];
        console.log("LevelCriteria", levelCriteria);
        console.log("SubCriteria", subCriteria);
        console.log('DrillDown', criteria);
        if (levelCriteria.Criteria != criteriaId)
            subCriteria.push(levelCriteria.Criteria);
        subCriteria = _.map(subCriteria, function (criteriumId) { return { Id: criteriumId, Selected: true, Name: Labels.criteria[criteriumId].Name }; });

        LocalStorage.cacheOrGenerate('GetCriteriaValueId', { criteriaId: criteriaId, name: title }, function () {
            return Service('GetCriteriaValueId', { criteriaId: criteriaId, name: title });
        }).then(function (guid) {
            var selectedCriteriaFilter = { Id: guid, Name: title };

            $modal.open({
                animation: false,
                templateUrl: 'drillDownContent.html',
                controller: function ($scope, $modalInstance) {
                    $scope.drillDown = {
                        Criteria: criteria,
                        OtherCriteria: otherCriteria ? Labels.criteria[otherCriteria.Criteria] : null,
                        SelectedCriteriaFilter: selectedCriteriaFilter,
                        SubCriteria: subCriteria
                    };
                    $scope.canShowAds = function () {
                        // Cannot activate "Show Ads" if they are already shown
                        return !_.some(currentReporter.Criteria, function (c) { return c.Criteria === 9; }); // 9 is "Sample Title"
                    };
                    $scope.ok = function () {
                        var reporter;
                        switch ($scope.drillDown.mode) {
                            case 'drilldown':
                                reporter = buildDrillDownReporter($scope.drillDown, currentReporter);
                                break;
                            case 'showads':
                                reporter = buildShowAdsReporter($scope.drillDown, currentReporter);
                                break;
                        }
                        if ($scope.drillDown.Criteria.Id == 1) // Industry
                            reporter.Industries.push($scope.drillDown.SelectedCriteriaFilter);
                        if ($scope.drillDown.Criteria.Id == 2) // Advertiser
                            reporter.Companies.push($scope.drillDown.SelectedCriteriaFilter);
                        if ($scope.drillDown.Criteria.Id == 4) // Channel
                            reporter.Channels.push($scope.drillDown.SelectedCriteriaFilter);
                        getReport(reporter);

                        $modalInstance.close();
                    };
                    $scope.cancel = function () {
                        $modalInstance.close();
                    };
                }
            });
        });
    }
    function buildDrillDownReporter(drillDown, currentReporter) {
        var selectedSubCriteria = _.where(drillDown.SubCriteria, { Selected: true });
        if (selectedSubCriteria.length == 0)
            selectedSubCriteria.push(drillDown.SubCriteria[0]); // At least one criteria is needed for pivot rows
        var reporter = {
            Range: currentReporter.Range,
            Criteria: _.map(selectedSubCriteria, function (criteria) { return { Criteria: criteria.Id, Informative: [] }; }),
            Values: currentReporter.Values,
            Industries: currentReporter.Industries,
            Companies: [],
            Channels: [],
            Pivot: true
        };
        if (drillDown.OtherCriteria)
            reporter.Criteria.push({ Criteria: drillDown.OtherCriteria.Id, Informative: [] });

        return reporter;
    }
    function buildShowAdsReporter(drillDown, currentReporter) {
        var reporter = {
            Range: currentReporter.Range,
            Criteria: currentReporter.Criteria,
            Values: currentReporter.Values,
            Industries: currentReporter.Industries,
            Companies: [],
            Channels: []
        };
        var alreadyShownInfoCriteria = _.flatten(_.map(reporter.Criteria, function (criteria) { return criteria.Informative; }));
        var alreadyShownCriteria = _.union(_.pluck(reporter.Criteria, 'Criteria'), alreadyShownInfoCriteria);
        var remainingInformative = _.difference(Labels.criteria[9].ParentIds, alreadyShownCriteria);
        reporter.Criteria.push({ Criteria: 9, Informative: remainingInformative });

        return reporter;
    }
}]);
