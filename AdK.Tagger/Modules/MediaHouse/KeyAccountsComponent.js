function KeyAccountsComponentController($element, Brandvertiser, Paged, debounce) {
	var ctrl = this;

	ctrl.filter = '';
	ctrl.brandOrAdvertiser = 3;
	ctrl.current = {
		brandvertiser: null,
		keyAccount: null
	};

	ctrl.brandvertisers = [null];

	function getPage(pageNum) {
		return Brandvertiser.getPage(ctrl.brandOrAdvertiser === 1 || ctrl.brandOrAdvertiser == 3, ctrl.brandOrAdvertiser === 2 || ctrl.brandOrAdvertiser == 3, pageNum, ctrl.filter).then(function (page) {
			return {
				TotalCount: page.TotalCount,
				Items: page.Brandvertisers
			};
		});
	}

	function filterChanged() {
		var paged = new Paged(Brandvertiser.pageSize, getPage);
		ctrl.brandvertisers = paged.items;
		ctrl.getIndexed = paged.need;
		paged.need(0);
	}
	ctrl.filterChanged = debounce(500, filterChanged);

	ctrl.addKeyAccount = function () {
		if (ctrl.current.brandvertiser) {
			Brandvertiser.addKeyAccount(ctrl.current.brandvertiser);
			ctrl.keyAccounts.push(ctrl.current.brandvertiser);
			ctrl.current.keyAccount = ctrl.current.brandvertiser;
			ctrl.current.brandvertiser = null;
		}
	};
	ctrl.removeKeyAccount = function () {
		if (ctrl.current.keyAccount) {
			Brandvertiser.removeKeyAccount(ctrl.current.keyAccount);
			_.remove(ctrl.keyAccounts, ctrl.current.keyAccount);
			ctrl.current.brandvertiser = ctrl.current.keyAccount;
			ctrl.current.keyAccount = null;
		}
	};
	ctrl.canAdd = function () {
		return ctrl.current.brandvertiser && !_.some(ctrl.keyAccounts, 'Id', ctrl.current.brandvertiser.Id);
	};
	ctrl.canRemove = function () {
		return ctrl.current.keyAccount;
	};

	Brandvertiser.getKeyAccounts().then(function (keyAccounts) {
		ctrl.keyAccounts = keyAccounts;
	});

	filterChanged();
}




angular.module('app')
	.component('keyAccounts',
		{
			templateUrl: '/Modules/MediaHouse/KeyAccountsComponent.html',
			bindings: {

			},
			controller: ['$element', 'Brandvertiser', 'Paged', 'debounce', KeyAccountsComponentController]
		});
