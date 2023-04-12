ACS12 - User Contract Standard
==============================

ACS12 is used to manage user contract.


Types
-----

.. raw:: html

   <div id="acs12.UserContractMethodFees">

.. raw:: html

   </div>

acs12.UserContractMethodFees
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

+-------------------+-------------------------------------------------------------------+-------------+----------------------------------------------------------------------+
| Field             | Type                                                              | Label       | Description                                                          |
+===================+===================================================================+=============+======================================================================+
| fees              | `acs12.UserContractMethodFee <#acs12.UserContractMethodFee>`__    | repeated    | List of fees to be charged.                                          |
+-------------------+-------------------------------------------------------------------+-------------+----------------------------------------------------------------------+
| is_size_fee_free  | `bool <#bool>`__                                                  |             | Optional based on the implementation of SetConfiguration method.     |
+-------------------+-------------------------------------------------------------------+-------------+----------------------------------------------------------------------+

.. raw:: html

   <div id="acs12.UserContractMethodFee">

.. raw:: html

   </div>

acs12.UserContractMethodFee
~~~~~~~~~~~~~~~~~~~~~~~~~~~~

========= ==================== ===== ===================================
Field     Type                 Label Description
========= ==================== ===== ===================================
symbol    `string <#string>`__       The token symbol of the method fee.
basic_fee `int64 <#int64>`__         The amount of fees to be charged.
========= ==================== ===== ===================================