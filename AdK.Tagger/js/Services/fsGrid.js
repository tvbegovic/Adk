angular.module('app')
  .service('fs', [function() {
    return fsGridModule;
  }])
  .filter('fsFilterItem', [function() {
    return function(items, grid) {
      return _.filter(items, function(item) { return grid.filterItem(item); });
    };
  }])
  .directive('fsSortIndicator', [function() {
    return {
      restrict: 'E',
      replace: true,
      template: '<i class="glyphicon pull-right" ng-show="isSorted()" ng-class="{true: \'glyphicon-chevron-up\', false: \'glyphicon-chevron-down\'}[column.grid.sort.ascending]" ng-click="setSort()"></i>',
      scope: {
        column: '='
      },
      link: function(scope) {
        var c = scope.column;

        scope.setSort = function() {
          c.grid.toggleSorting(c);
        };
        scope.isSorted = function() {
          return c.grid.sort.column === c;
        };
      }
    };
  }])
  .directive('fsHeader', ['focus', function(focus) {
    return {
      restrict: 'E',
      replace: true,
      template: '\
<div>\
	<div class="dropdown pull-right" dropdown dropdown-append-to-body dropdown-toggle ng-if="column.filter.choices">\
		<i class="glyphicon glyphicon-filter" ng-class="{inactive: !column.isFilterActive()}"></i>\
		<ul class="dropdown-menu">\
			<li ng-repeat="choice in column.filter.choices" ng-class="{active: column.filter.filteredBy === choice}"><a ng-click="column.filter.set(choice)">{{choice.label}}</a></li>\
		</ul>\
	</div>\
	<div class="col-label">\
		<div ng-if="column.filter.substring != undefined" ng-switch="column.filter.editing">\
			<div ng-switch-when="false" ng-click="setSort()">\
				<i class="glyphicon glyphicon-filter pull-right" ng-class="{inactive: !column.isFilterActive()}" ng-click="startFilterEditing()"></i>\
				<fs-sort-indicator column="column"></fs-sort-indicator>\
				{{column.header}}\
			</div>\
			<div ng-switch-when="true">\
				<input type="search" class="filter-input" ng-model="column.filter.substring" placeholder="{{column.header}}" results="5" on-enter="column.filter.editing = false" focus-on="filter-input-{{column.attribute}}" />\
			</div>\
		</div>\
		<div ng-if="column.filter.substring == undefined" ng-click="setSort()">\
			<fs-sort-indicator column="column"></fs-sort-indicator>\
			{{column.header}}\
		</div>\
	</div>\
</div>',
      scope: {
        column: '='
      },
      link: function(scope) {
        var c = scope.column;

        scope.setSort = function() {
          c.grid.toggleSorting(c);
        };
        scope.startFilterEditing = function() {
          c.filter.editing = true;
          focus('filter-input-' + c.attribute);
        };
      }
    };
  }]);
