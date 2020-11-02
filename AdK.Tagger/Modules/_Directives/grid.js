angular.module('app')
  .directive('grid', ['Pager','focus',function (Pager,focus) {
      return {
          restrict: 'AE',
          scope: {
              def: '=',
              selected: '=?selected',
              onSelected: '=?onselected',
			  filterTypeahead: '@?filterautocomplete',
			  rowFilter: '=?rowfilter',
			  data: '='
          },
          link: function($scope, element, attrs) {
              $scope.selectedRow = null;
              //$scope.filterTypeahead = false;
              $scope.activeFilter = '';
              /*$scope.filter = { Country: '', City: '',StationName: '',sort: {} };*/
              //$scope.FilterSuggestions = { 'Country': [], 'City': [], 'StationName' : [] };
              $scope.currentData= [];
			  //$scope.dataFiltered = [];
			  
			  $scope.sort = $scope.def.sort != null ? $scope.def.sort : { column: '', ascending: true };

			  $scope.def.columns.forEach(function (c) {
				  c.filter = '';
			  });
			  if ($scope.def.selectionType == null)
				  $scope.def.selectionType = 'single';
			  if ($scope.def.selectionField == null)
				  $scope.def.selectionField = 'selected';

			  if ($scope.def.pager == null)
				  $scope.def.pager = true;

              $scope.rowClicked = function(r)
			  {
				  if ($scope.selectionType == 'single') {
					  if ($scope.selectedRow != null)
						  $scope.selectedRow[$scope.selectionField] = false;
					  r[$scope.selectionField] = true;
					  $scope.selectedRow = r;
				  }                  
                  if ($scope.onSelected != null)
                      $scope.onSelected(r);
              };
              $scope.pager = new Pager();

              $scope.setFocus = function (column) {
                  focus('Filter_' + column.field);
                  $scope.activeFilter = column.field;
              };

              $scope.setSort = function (column) {
                  if ($scope.sort.column == column) {
                      $scope.sort.ascending = !$scope.sort.ascending;
                  } else {
                      $scope.sort.column = column;
                      $scope.sort.ascending = true;
				  }
				  $scope.sortData();
                  $scope.getData();
              };

              $scope.filterOnSelect = function ()
              {
                  $scope.filterOnEnter();
                  $scope.getData();
              }

			  $scope.getData = function (keepPagerIndex) {
				  if (!keepPagerIndex) {
					  $scope.pager.reset();
				  }

				  var pageSize = $scope.pager.size;
				  var pageNum = $scope.pager.index - 1;

				  var sliceStart = $scope.def.pager ? pageNum * pageSize : 0;

				  if ($scope.data != null) {
					  var dataFiltered = $scope.data;

					  _.filter($scope.def.columns, { hasFilter: true }).forEach(function (c) {
						  if (c.filter)
							  dataFiltered = dataFiltered.filter(function (d) {
								  return d[c.field].search(new RegExp(c.filter, "i")) >= 0;
							  });
					  });

					  var sliceStop = $scope.def.pager ? sliceStart + pageSize : dataFiltered.length;

					  $scope.currentData = dataFiltered.slice(sliceStart, sliceStop);

					  if ($scope.selectedRow != null && _.find($scope.currentData, function (d) { return d[$scope.def.idField] == $scope.selectedRow[$scope.def.idField]; }) == null) {
						  $scope.selectedRow.Selected = false;
						  $scope.selected = null;
					  }
					  $scope.pager.setItemCount(dataFiltered.length);
				  } else {
					  $scope.currentData = [];
				  }


                  
              };

              $scope.$watchGroup(['pager.index', 'pager.size'], function () { $scope.getData(true); });

			  $scope.$watch('data', function () {

				  if ($scope.data != null) {
					  $scope.def.columns.forEach(function (c) {
						  c.filterSuggestions = _.uniq(_.map($scope.data, c.field));
					  });
					  $scope.sortData();
					  $scope.getData(true);
				  } else {
					  if ($scope.currentData != null)
						  $scope.currentData = [];
				  }
			  }, true);

			  $scope.sortData = function () {
				  if ($scope.data && $scope.sort.column.length > 0) {
					  $scope.data.sort(function (a, b) {
						  if ($scope.sort.ascending)
							  if (a[$scope.sort.column] > b[$scope.sort.column])
								  return 1;
							  else if (a[$scope.sort.column] < b[$scope.sort.column])
								  return -1
							  else return 0;
						  else
							  if (a[$scope.sort.column] < b[$scope.sort.column])
								  return 1;
							  else if (a[$scope.sort.column] > b[$scope.sort.column])
								  return -1
							  else return 0;
					  });
				  }
				  
			  }

              $scope.getItems = function (column, search) {
                  var searchLowerCase = search.toLowerCase();
                  return column.filterSuggestions.filter(function (fs) {
                      return fs.toLowerCase().includes(searchLowerCase);
                  });
              };

              $scope.filterOnEnter = function () {
                  $scope.activeFilter = '';
              };

              $scope.clearFilter = function (column) {
                  column.filter = '';
                  $scope.getData();
			  };

			  $scope.isFilterTypeahead = function (c) {
				  return ($scope.filterTypeahead && c.filterTypeahead != false) || (!$scope.filterTypeahead && c.filterTypeahead == true);
			  }

			  $scope.dataFilter = function (d) {
				  if ($scope.rowFilter != null)
					  return $scope.rowFilter(d);
				  return true;
			  }
          },
          templateUrl: '/Modules/_Directives/grid.html'
      };
  }]);
