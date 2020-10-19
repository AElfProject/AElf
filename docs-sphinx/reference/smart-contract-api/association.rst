AElf.Contracts.Association
--------------------------

Association contract.

Organizations established to achieve specific goals can use this
contract to cooperatively handle transactions within the organization

Implement AElf Standards ACS1 and ACS3.

+---------------+-----------------+------------------+-----------------+
| Method Name   | Request Type    | Response Type    | Description     |
+===============+=================+==================+=================+
| Creat         | `Creat          |                  | Create an       |
| eOrganization | eOrganizationIn | .aelf.Address    | organization    |
|               | put <#Associati | aelf.Address     | and return its  |
|               | on.CreateOrgani |                  | address.        |
|               | zationInput>`__ |                  |                 |
+---------------+-----------------+------------------+-----------------+

.. container::
   :name: Association.CreateOrganizationInput

Association.CreateOrganizationInput
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

+-------------+----------+-------------+------------------------------+
| Field       | Type     | Label       | Description                  |
+=============+==========+=============+==============================+
| or          | Organiz  |             | Initial organization         |
| ganization_ | ationMem |             | members.                     |
| member_list | berList  |             |                              |
+-------------+----------+-------------+------------------------------+
