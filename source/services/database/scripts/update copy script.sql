update f1 set f1.Name = f2.Name from Fields f1 inner join FieldTypes f2 on f1.FieldTypeID = f2.FieldTypeID where f1.FieldTypeID = f2.FieldTypeID
