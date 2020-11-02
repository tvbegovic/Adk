angular.module('app')
    .controller('clientsContactsCtrl', ['$scope', '$modal','$q','$timeout', 'Service', 'Pager', 'confirmPopup','cookieService','focus',
		function ($scope, $modal, $q, $timeout, Service, Pager, confirmPopup, cookieService, focus) {

			$scope.tabs = {
				'client': { active: true },
				'contact': {active: false}
			};

			$scope.clientGridDef = {
				columns: [
					{ field: 'checked', name: '', hasFilter: false, hasSort: false, cssStyle: {width: '30px'}},
					{ field: 'name', name: 'Client Name', filter: '', hasFilter: true, filterTypeahead: false }					
				],
				selectionType: 'multi',
				//sort: { column: 'name', ascending: true },
				selectionColumnIndex: 0,
				idField: 'Id',
				pager: false,
				fixedHeader: true,
				fixedClass: 'clientGridFixed',
				scrollY: '66vh'
			};

			$scope.contactGridDef = {
				columns: [
					{ field: 'checked', name: '', hasFilter: false, hasSort: false, cssStyle: { width: '30px' } },
					{ field: 'name', name: 'Name', filter: '', hasFilter: true, filterTypeahead: false },
					{ field: 'email', name: 'Email', filter: '', hasFilter: true, filterTypeahead: false }
				],
				selectionType: 'multi',
				//sort: { column: 'name', ascending: true },
				selectionColumnIndex: 0,
				idField: 'Id',
				pager: false,
				fixedHeader: true,
				fixedClass: 'contactGridFixed',
				scrollY: '23vh'
			};

			$scope.feedsGridDef = {
				columns: [
					{ field: 'checked', name: '', hasFilter: false, hasSort: false, cssStyle: { width: '30px' } },
					{ field: 'Client', name: 'Name', filter: '', hasFilter: true, filterTypeahead: false },
					{ field: 'cm', name: 'Country/Market', filter: '', hasFilter: true, filterTypeahead: false }
				],
				selectionType: 'multi',
				selectionColumnIndex: 0,
				sort: { column: 'Client', ascending: true },
				idField: 'Id',
				pager: false,
				fixedHeader: true,
				fixedClass: 'feedGridFixed',
				scrollY: '26vh'
			};
			var newTermId = -1;
			$scope.ClientSearchTerms = [];
			$scope.ContactSearchTerms = [];

			Service('GetClients', null, { backgroundLoad: true }).then(function (clients) {
				clients.forEach(d => {
					d.checked = false;
				});
				
				Service('GetFeeds', { checkNew: false }, { backgroundLoad: true }).then(function (data) {
					prepareFeeds(data.feeds);
					$scope.feeds = data.feeds;
					clients.forEach(c => {
						if (c.feeds.length > 0) {
							var feedIds = c.feeds.map(f => f.Id);
							c.feeds = JSON.parse(JSON.stringify(data.feeds.filter(f=>feedIds.indexOf(f.Id) >= 0)));
						}
					});
					$scope.clients = clients;
					var lastClient = cookieService.get('clients_selectedClient');
					if (lastClient != null) {
						$scope.client = _.find($scope.clients, { id: parseInt(lastClient) });
						if ($scope.client != null)
							$scope.clientSelected($scope.client);
					}
				});


				Service('GetCommonSearchTerms').then(function (terms) {
					$scope.commonsearches = terms;
				});
                
                    
            });

			
			

            $scope.rowClass = function (r) {
                return r.selected ? 'selected' : '';
            }

            $scope.clientSelected = function (c) {
                unSelect($scope.clients);
                c.selected = true;
				cookieService.put('clients_selectedClient', JSON.stringify(c.id));
				if ($scope.client != null && $scope.client.contact != null && $scope.client.contact.feeds != null) {
					$scope.client.contact.feeds.splice(0, $scope.client.contact.feeds.length);
				}
				if ($scope.client != null && $scope.client.contact != null) {
					if ($scope.client.contact.feeds != null)
						$scope.client.contact.feeds = [];
					$scope.client.contact = null;
				}
				$scope.tabs['client'].active = true;
				

				$scope.client = c;
				
				Service('GetContactsForClient', { client_id: c.id }, { backgroundLoad: true }).then(function (data) {
					$scope.client.contacts = data;
					var lastContact = cookieService.get('clients_selectedContact');

					if (lastContact != null) {
						$scope.client.contact = _.find($scope.client.contacts, { contact_id: parseInt(lastContact) });
						if ($scope.client.contact != null)
							$scope.contactSelected($scope.client.contact);
					}
				});
				if (c.feeds == null) {
					Service('GetFeedsForClient', { client_id: c.id }, { backgroundLoad: true }).then(function (data) {
						prepareFeeds(data);
						c.feeds = data;
					});
				}
				Service('GetSearchPhrasesForClient', { client_id: c.id }, { backgroundLoad: true }).then(function (data) {
					$scope.ClientSearchTerms = data;
				});
								
			}

			$scope.clientsChecked = function () {
				return _.find($scope.clients, { checked: true }) != null;
			};

			function getCheckedClients() {
				return _.filter($scope.clients, { checked: true });
			}

			$scope.contactsChecked = function () {
				if ($scope.client != null)
					return _.find($scope.client.contacts, { checked: true }) != null;
				return false;
			};

			function getCheckedContacts() {
				return _.filter($scope.client.contacts, { checked: true });
			}

            function unSelect(array) {
                array.forEach(function (elem) {
                    elem.selected = false;
                });
			}

			function unCheck(array) {
				array.forEach(e => e.checked = false);
			}

            $scope.addClient = function () {
                var client = { id: null, name: '' };
                addEditClient(client).then(function (client) {
                    $scope.clients.push(client);
                });
            };

            $scope.addClient = function () {
                var client = { id: 0, name: '' };
                addEditClient(client).result.then(function (client) {
					if (client != null) {
						$scope.clients.push(client);
						$scope.clientSelected(client);
					}
						
                });
            };

            $scope.editClient = function (c) {
                addEditClient(c).result.then(function (client) {
                    if (client != null) {
                        c.name = client.name;
                    }

                });
            };

            $scope.deleteClients = function (c)
			{
				confirmPopup.open('Delete clients', null, 'Are you sure you want to delete selected clients?').then(function () {
					var checkedClients = getCheckedClients();
					var ids = checkedClients.map(c => c.id);
					Service('DeleteClients', { ids: ids.join(',') }).then(data => {
						var id = ids[ids.length - 1];
						var currentDeleted = $scope.client != null ? ids.indexOf($scope.client.id) >= 0 : false;
						var position = -1;
						if (currentDeleted)
							position = $scope.clients.findIndex(c => c.id == $scope.client.id);
						_.remove($scope.clients, c => ids.indexOf(c.id) >= 0);

						
						if (currentDeleted) {
							if ($scope.client.contacts != null) {
								$scope.client.contacts = [];
								if ($scope.client.contact != null && $scope.client.contact.feeds != null)
									$scope.client.contact.feeds = [];
							}
							if ($scope.clients.length > 0) {
								if (position >= $scope.clients.length)
									position--;
								$scope.clientSelected($scope.clients[position]);
							}
							else {
								if ($scope.client != null)
									$scope.client.contact = null;
							}
						}
						
							
                        
                    },
                    statusText => $scope.errorMessage = statusText);
                        
                });
                
            }

            function addEditClient(client)
            {
                return $modal.open({
                    animation: false,
                    templateUrl: 'tplClientEdit.html',
                    controller: ['$scope', '$modalInstance','client','title',function ($scope, $modalInstance, client,title) {
                        $scope.client = angular.copy(client);
                        $scope.title = title;
						focus('clientName');
                        $scope.ok = function () {
                            if ($scope.form.$valid) {
                                var method = client.id == 0 ? 'CreateClient' : 'UpdateClient';
                                Service(method, { c: $scope.client }).then(function (data) {
                                    $modalInstance.close(data);
                                });
                            }
                            else
                                $scope.form.$setDirty();
                        };

                        $scope.cancel = function () {
                            $modalInstance.close();
                        };
                    }],
                    resolve: {
                        client: function () {
                            return client;
                        },
                        title: function () {
							return client.id == 0 ? 'Add new client' : 'Edit ' + client.name
                        }
                    }
                });
            }

            $scope.addContact = function () {
                var contact = { contact_id: 0, user_id: '', email: '', client_id: $scope.client.id };
                getUsers().then(function () {
                    addEditContact(contact).result.then(function (contact) {
                        if (contact != null)
                        {
                            if ($scope.client.contacts == null)
								$scope.client.contacts = [];
							var user = $scope.users.find(u => u.Id == contact.user_id);
							if (user != null) {
								user.hasContact = true;
							}
							$scope.client.contacts.push(contact);
							$scope.contactSelected(contact);
                        }
                            
                    });
                });               
                
            };

			

            $scope.editContact = function (c) {
                getUsers().then(function () {
                    addEditContact(c).result.then(function (contact) {
						if (contact != null) {
							if (c.user_id != contact.user_id) {
								var user = $scope.users.find(u => u.Id == c.user_id);
								if (user != null) {
									user.hasContact = false;
								}
								user = $scope.users.find(u => u.Id == contact.user_id);
								if (user != null)
									user.hasContact = true;
							}
                            c.user_id = contact.user_id;
                            c.email = contact.email;
                            c.name = contact.name;
                        }
					});

                });                
            };

            function getUsers()
            {
                return $q(function (resolve, reject) {
                    if ($scope.users == null)
						Service('GetAvailableUsers').then(function (data) {
							$scope.users = data;
                            resolve();
                        });
                    else
                        resolve();
                });
			}

			$scope.deleteContacts = function (c) {
				confirmPopup.open('Delete contacts', null, 'Are you sure you want to delete selected contacts?').then(function () {
					var checkedContacts = getCheckedContacts();
					var ids = checkedContacts.map(c => c.contact_id);
					Service('DeleteContacts', { ids: ids.join(',') }).then(data => {
						var id = ids[ids.length - 1];
						var currentDeleted = $scope.client != null && $scope.client.contact != null ? ids.indexOf($scope.client.contact.contact_id) >= 0 : false;
						var position = -1;
						if (currentDeleted)
							position = $scope.client.contacts.findIndex(c => c.contact_id == $scope.client.contact.contact_id);
						if($scope.client != null)
							_.remove($scope.client.contacts, c => ids.indexOf(c.contact_id) >= 0);
						
						if (currentDeleted) {
							if ($scope.client.contact != null && $scope.client.contact.feeds != null)
								$scope.client.contact.feeds = [];
							
							if ($scope.client.contacts.length > 0) {
								if (position >= $scope.client.contacts.length)
									position--;
								$scope.contactSelected($scope.client.contacts[position]);
							}
							else {
								if ($scope.client != null)
									$scope.client.contact = null;
							}
						}



					},
						statusText => $scope.errorMessage = statusText);

				});

			}

            $scope.removeContact = function (c) {
                confirmPopup.open('Delete contact', null, 'Are you sure you want to delete this contact?').then(function () {
					Service('DeleteContact', { id: c.contact_id }).then(function (data) {
						$scope.client.contact.feeds = [];
						
						var position = _.findIndex($scope.client.contacts, { contact_id: c.contact_id });
						_.remove($scope.client.contacts, { contact_id: c.contact_id });
						if ($scope.client.contacts.length > 0) {
							if (position >= $scope.client.contacts.length)
								position--;
							$scope.contactSelected($scope.client.contacts[position]);
						}
						else
							$scope.client.contact = null;
                    },
                        function (statusText) {
                            $scope.errorMessage = statusText;
                        });
                });

            }

            function addEditContact(contact) {
				return $modal.open({
                    animation: false,
                    templateUrl: 'tplContactEdit.html',
                    controller: ['$scope', '$modalInstance', 'contact', 'title','users',function ($scope, $modalInstance, contact, title, users) {
                        $scope.contact = angular.copy(contact);
                        $scope.title = title;
						$scope.users = users;
						$scope.enableValidation = false;
						focus('')
                        if (contact.contact_id > 0)
                        {
                            var index = _.findIndex($scope.users, { Id: contact.user_id });
                            if (index >= 0)
                                $scope.contact.user = $scope.users[index];
                        }
							
                        
                        $scope.ok = function () {
							if ($scope.form.$valid) {

								if ($scope.contact.user_id != null && $scope.contact.user_id.length > 0) {
									Service('CheckContactUser', { user_id: $scope.contact.user_id, contact_id: $scope.contact.contact_id }).then(function (data) {
										if (data)
											$scope.errorMessage = 'User is already associated with another contact';
										else {
											SaveContact();
										}
									});
								}
								else
									SaveContact();


							}
							else {
								$scope.enableValidation = true;
							}
						};

						$scope.showValidation = function () {
							return $scope.enableValidation && !$scope.form.$valid;
						}

						$scope.filterUsers = function (u) {
							if ($scope.contact.contact_id > 0)
								return true;
							return u.hasContact == false;
						}

                        function SaveContact()
                        {
                            var method = $scope.contact.contact_id == 0 ? 'CreateContact' : 'UpdateContact';

                            Service(method, { c: $scope.contact }).then(function (data) {
                                $modalInstance.close(data);
                            },
                                function (errResponse) {
                                    $scope.errorMessage = errResponse.data.Message;
                                }
                            );
                        }

                        $scope.userSelected = function () {
                            $scope.contact.user_id = $scope.contact.user.Id;
							$scope.contact.email = $scope.contact.user.Email;
							$scope.contact.name = $scope.contact.user.slug;
                        };

                        $scope.cancel = function () {
                            $modalInstance.close();
                        };
                    }],
                    resolve: {
                        contact: function () {
                            return contact;
                        },
                        title: function () {
							return contact.contact_id == 0 ? 'Add new contact' : 'Edit ' + contact.name;
                        },
                        users: function () {
                            return $scope.users;
                        }
                    }
                });
            }

            $scope.contactSelected = function (c) {
                unSelect($scope.client.contacts);
                c.selected = true;
				$scope.client.contact = c;
				unCheck($scope.feeds);
				
				cookieService.put('clients_selectedContact', c.contact_id);
				
				Service('GetFeedsForContact', { contact_id: c.contact_id }, { backgroundLoad: true }).then(data => {
					var contactFeeds = angular.copy(_.filter($scope.feeds, f => {
						return _.findIndex(data, { Id: f.Id }) >= 0;
					}));
					if (c.feeds == null)
						c.feeds = [];
					c.feeds.splice(0, c.feeds.length);
					contactFeeds.forEach(f => c.feeds.push(f));
				});


				Service('GetSearchPhrasesForContact', { contact_id: c.contact_id }, { backgroundLoad: true }).then(function (data) {
					$scope.ContactSearchTerms = data;
				});

				
				
            }

            $scope.feedToggle = function (f) {
                f.selected = !f.selected;
            }

            $scope.feedFilter = function (f) {
				if($scope.client != null && $scope.client.contact != null)
                    return !_.find($scope.client.contact.feeds, { Id: f.Id });
                return true;
			}

			$scope.feedsChecked = function () {
				if($scope.client != null)
					return _.find($scope.client.feeds, { checked: true }) != null;
				return false;
				
			};

			$scope.contactFeedsChecked = function () {
				if ($scope.client != null && $scope.client.contact != null) {
					return _.find($scope.client.contact.feeds, { checked: true }) != null;
				}
				return false;
			};

			function getClientFeedsChecked() {
				return _.filter($scope.client.feeds, { checked: true });
			}

            $scope.assign = function () {
                if ($scope.client.contact.feeds == null)
                    $scope.client.contact.feeds = [];
				var checked = getClientFeedsChecked();
				var selected = checked;
				if ($scope.client.contact.feeds.length > 0) {
					var contactFeedIds = $scope.client.contact.feeds.map(f => f.Id);
					selected = _.filter(selected, f => contactFeedIds.indexOf(f.Id) < 0);
				}
				var ids = _.map(selected, 'Id').join(',');
				if (selected.length > 0) {
					Service('AddFeedsToContact', { contact_id: $scope.client.contact.contact_id, feedIds: ids }, { backgroundLoad: true }).then(function () {
						selected.forEach(function (f) {
							f.checked = false;
							$scope.client.contact.feeds.push(angular.copy(f));
						});
					});
				}
				else {
					checked.forEach(f => f.checked = false);
				}
				
                
            };

            $scope.unassign = function () {
                var selected = _.filter($scope.client.contact.feeds, { checked: true });
                var ids = _.map(selected, 'Id').join(',');
				Service('RemoveFeedsFromContact', { contact_id: $scope.client.contact.contact_id, feedIds: ids }, { backgroundLoad: true }).then(function () {
                    _.remove($scope.client.contact.feeds, { checked: true });    
                });
                
            };

            $scope.selectedFeedCount = function (type)
            {
                return type == 0 ? _.filter($scope.feeds, { selected: true }).length :
                    $scope.client != null && $scope.client.contact != null ? _.filter($scope.client.contact.feeds, { selected: true }).length : 0;
            }

            $scope.removeClientFeeds = function ()
            {
				confirmPopup.open('Delete feeds', null, 'Are you sure you want to remove selected feeds from this client?').then(function () {
					
					var ids = getClientFeedsChecked().map(f => f.Id).join(',');

					Service('RemoveFeedsFromClient', { client_id: $scope.client.id, feedIds: ids }).then(function () {                        
						_.remove($scope.client.feeds, { checked: true });
						$scope.client.contacts.forEach(c => {
							if (c.feeds != null)
								_.remove(c.feeds, f => ids.indexOf(f.Id) >= 0);
						});
                    },
                        function (errorText) {
                            $scope.errorMessage = errorText;
                        }
                    );
                });
                
			}

			function prepareFeeds(data) {
				data.forEach(d => {
					d.LastTimestamp = d.LastTimestamp != null ? moment(d.LastTimestamp).toDate() : null;
					d.cm = d.Market + ' ' + d.Domain;
				});
			}

			$scope.addFeed = function () {
				var modalInstance = $modal.open({
					animation: false,
					templateUrl: 'tplAddFeed.html',
					controller: ['$scope', '$modalInstance', 'params', function ($scope, $modalInstance, params) {

						$scope.feeds = angular.copy(params.feeds);
						$scope.cancel = function () {
							$modalInstance.close();
						}
						var client = params.client;
						$scope.feedsGridDef = params.feedsGridDef;

						function getCheckedFeeds() {
							return _.filter($scope.feeds, { checked: true });
						};

						$scope.feedFilter = function (f) {
							if ($scope.search && $scope.search.length > 0) {
								var regExp = new RegExp($scope.search, "i");
								return f.Client.match(regExp) || f.cm.match(regExp);
							}
								
							return true;
						}

						$scope.save = function () {
							var feeds = getCheckedFeeds();
							feeds.forEach(f => f.checked = false);
							if (feeds.length > 0) {
								Service('AddFeedsToClient', { client_id: client.id, feedIds: feeds.map(f => f.Id).join(',') }).then(function (data) {
									$modalInstance.close(feeds);
								},
									function (errResponse) {
										$scope.errorMessage = errResponse.data.Message;
									}
								);
							}
							else
								$modalInstance.close();

						}


					}],
					resolve: {
						params: function () {
							return {
								feeds: _.filter($scope.feeds, f => $scope.client.feeds.map(cf => cf.Id).indexOf(f.Id) < 0),
								client: $scope.client,
								feedsGridDef: $scope.feedsGridDef
							}
						}						
					}
				});

				modalInstance.result.then(feeds => {
					if (feeds != null && feeds.length > 0) {
						feeds.forEach(f => $scope.client.feeds.push(f));
					}

				});
			};

			$scope.clientSearchTermAdded = function(obj) {
				obj.client_id = $scope.client.id;
				obj.searchTerm = obj.search_term.trim();
				Service('AddSearchPhraseToClient', { cst: obj }, { backgroundLoad: true }).then(function (newData) {
					obj.id = newData.id;
					obj.Common = newData.Common;
				});
			}

			$scope.clientSearchTermAdding = function (obj) {
				obj.id = newTermId--;
				return true;
			}

			$scope.clientSearchTermRemoved = function (obj) {
				if (obj.id != null) {
					Service('RemoveSearchPhraseFromClient', { id: obj.id }, { backgroundLoad: true }).then(function () { });
				}
			}

			$scope.contactSearchTermAdded = function (obj) {
				obj.contact_id = $scope.client.contact.contact_id;
				delete obj["__type"];
				Service('AddSearchPhraseToContact', { cst: obj }, { backgroundLoad: true }).then(function (id) {
					obj.id = id;
				});
			}

			$scope.contactSearchTermAdding = function (obj) {
				obj.id = newTermId--;
				return true;
			}

			$scope.contactSearchTermRemoved = function (obj) {
				if (obj.id != null) {
					Service('RemoveSearchPhraseFromContact', { id: obj.id }, { backgroundLoad: true }).then(function () { });
				}
			}

			$scope.addCommonSearchToClient = function () {
				if (this.selectedCommonSearch != null) {
					var tag = { client_id: $scope.client.id, common_id: this.selectedCommonSearch.id, search_term: this.selectedCommonSearch.name };
					$scope.ClientSearchTerms.push(tag);
					$scope.clientSearchTermAdded(tag);
				}
			}

			$scope.clientSearchTagClass = function($tag, $index, $selected) {
				var c = 'tag-item';
				if ($tag.common_id != null) {
					c += ' tag-common';				
				}
				return c;
			}

			$scope.getTerms = function (term) {
				return Service('GetClientContactSearchTerms', { term: term }, {backgroundLoad: true});
			}

			$scope.tagTitle = function (tag) {
				return tag.Common != null ? tag.Common.Terms.map(function (t) { return t.name; }).join(', ') : '';
			}

			$scope.editTag = function (t) {
				if (t.Common == null) {
					t.Common = { Terms: [] };
				}
				var tagCopy = angular.copy(t);
				var modalInstance = $modal.open({
					animation: false,
					templateUrl: 'editTag.html',
					controller: ['$scope', '$modalInstance', 'params', function ($scope, $modalInstance, params) {
						$scope.tag = params.tag;
						var newId = -1;
						$scope.ok = function () {
							$modalInstance.close($scope.tag);
						};

						$scope.cancel = function () {
							$modalInstance.close();
						};

						$scope.termAdding = function (tag) {
							tag.id = newId;
							newId--;
						};
					}],
					resolve: {
						params: function () {
							return {
								tag: tagCopy
							}
						}
					}
				});

				modalInstance.result.then(tag => {
					Service('UpdateClientSearchTerm', { term: tag }).then(function (newData) {
						t.search_term = newData.search_term;
						t.common_id = newData.common_id;
						if (tag.Common.Terms.length > 0) {
							t.Common = newData.Common;
						}
					});
				});
			}
						
        }
    ]);
