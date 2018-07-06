As the name suggests, this will convert the DataSet to JSON output .

## When is this useful for you?
* You have a dataset and you need to convert all the datatables or selected set of tables in the dataset to JSON, then this package will help you achieve that with minimal effort

## Example
Let us assume that you have 2 datatables in a dataset
* Tables[0] - dtBooks
* Tables[1] - dtAuthors

You would like to convert these datatables to JSON objects

You need to add a configuration string and this string should be the first table of your dataset
* Tables[0] - dtConfig
* Tables[1] - dtBooks
* Tables[2] - dtAuthors

The syntax for the configuration string is as follows

"<n>:<Name of the root node><:single or :singlevar>"

* The first parameter indicates the position of the datatable in the dataset
* The second parameter indicates the name given to the datatable. This will appear as the name of the root node in the JSON object
* The third part of the configuration string is flag which lets the formatter know if the table returned has a single row output (:single) or a single cell output (:singlevar). This is an optional parameter.


## Sample Code
````
DataSet ds = new DataSet();
DataTable dtConfig = new DataTable();
dtConfig.Columns.Add("ConfigName");
dtConfig.Rows.Add("1:Books");

DataTable dtBooks = new DataTable();
dtBooks.Columns.Add("id");
dtBooks.Columns.Add("booktitle");
dtBooks.Rows.Add(1, "Some text");
dtBooks.Rows.Add(2, "Some text 2");

ds.Tables.Add(dtConfig);
ds.Tables.Add(dtBooks);

DataSet2JSON.Formatter.FormatDataSet(ds); 
````

## Explanation

* We have created a config table "dtConfig" to which we added the configuration string
* We created another table "dtBooks" and added 2 books into that table
* These 2 tables have been added to a DataSet "ds"
* The first table (Tables[0]) in the dataset is the configuration table and it contains the configuration string "1:Books".
* This means - we would like to convert the table "Tables[1]" to JSON and the root node for that JSON should be "Books"
* The output of the above code is as shown below

## Output

````
{
    "Books":[{
        "id":1,
        "booktitle":"Some text"
    },{
        "id":2,
        "booktitle":"Some text2"
    }]
}
````


##Real Time Example

````
-- SQL 
SELECT '1:Books';
SELECT id, booktitle FROM tbl_books;
````

-- Code Behind
* Call Stored Procedure and fill DataSet (ds) using DataAdapter
* Pass dataset to the DataSet2JSON Formatter to get the JSON string

````
DataSet2JSON.Formatter.FormatDataSet(ds); 
````

# Types of configuration settings and difference in JSON output

* change id to BookID and booktitle to Title in JSON string
````
-- SQL 
SELECT '1:Books';
SELECT id as BookID, booktitle as Title FROM tbl_books;

-- JSON Output
{
    "Books":[{
        "BookID":1,
        "Title":"Some text"
    },{
        "BookID":2,
        "Title":"Some text2"
    }]
}
````

* Data table returns only single row output - A scenario where the user is searching for a book by title and we have only 1 book available with that title
````
-- SQL 
SELECT '1:Books:single';
SELECT id as BookID, booktitle as Title FROM tbl_books WHERE booktitle = 'Some text2';

-- JSON Output
{
    "Books":{
        "BookID":2,
        "Title":"Some text2"
    }
}
````

* Data table returns only single cell output - A scenario where we would like to show a message to the user if the searched book is not available
````
-- SQL 
IF EXISTS(SELECT id from tbl_books where booktitle like '%Hi%') THEN
	select '1:Books';
	select id as BookID, booktitle as Title FROM tbl_books WHERE booktitle like '%Hi%';
ELSE
	select '1:ResponseDescription:singlevar';
	select 'No books found with the searched title' ResponseDescription;
END IF;

-- JSON Output
{
	"ResponseDescription":"No books found with the searched title"
}
````

### Let's consider you have a page with "Genre's" and you should display the books under each Genre

````
-- Scifi
----- Scifi book 1
----- Scifi book 2
-- Thriller
----- Thriller book 1
----- Thriller book 2
````


Let us see how we can do this. To achieve the output, we need JSON in a Parent and child levels.
We would like to view the above collection in the format shown below

### Expected JSON string
````
{
    "Genres":[{
				"id" : "1",
				"GenreName":"Scifi",
				"Books":[{
					"genreid":"1",
					"BookID":3,
					"Title":"Scifi book 1"
				},{
					"genreid":"1",
					"BookID":4,
					"Title":"Scifi book 2"
				}]
			},
			{
				"id" : "2",
				"GenreName":"Thriller",
				"Books":[{
					"genreid":"2",
					"BookID":5,
					"Title":"Thriller book 1"
				},{
					"genreid":"2",
					"BookID":6,
					"Title":"Thriller book 2"
				}]
			}]
}
````

To create a JSON string as shown above, we should create a relation between the two tables, tbl_genres and tbl_books.
Lets assume that *id* is the column in tbl_genres and this is used as a foreign key in the column *genreid* in tbl_books


The configuration string that helps us get the above string format is 

````
select '1:Genres!1^id:2^genreid~1^Books';
select id,GenreName from tbl_genres;
select genreid,id as BookID,booktitle as Title from tbl_books; 
````

* As discussed above, the configuration string is the Table[0]
* tbl_genres is Table[1]
* tbl_books is Table[2]

Let us try to understand the configuration string.

* Here we are using '!' as a seperator.
	* Left of ! (1:Genres) is the JSON root name and table data
	* Right of !(1^id:2^genreid~1^Books) is a **Relation** (this plays a key role in creating different types of JSON outputs)
	
Explaining Relation	(Right of !):
* The string which defines the relation has 2 parts separated by ~
* Left part (1^id:2^genreid) is the join condition which when converted to SQL will be **tbl_genres.id = tbl_books.genreid**
* Right part (1^Books) decides where the object should be created and the object name
* The result of the join will be appended to the root node with sub node name as Books

<# parent table>^<parent columnname>:<# child table>^<column name of child>~<# table where the object should be created>^<Object name>

: [colon this means equals]
~ [values after tilda will say where & how the json value should be stored]


### Let us see another example:

Configuration string.
````
select '1:Genres,4:UserDetails:single!1^id:2^genreid~1^Books|2^authorid:3^id~2^Author:single'; 
````

Let us read/understand this configuration.

Splitting the configuration into 2 parts by (!)
Right Side:
	> 1^id:2^genreid~1^Books|2^authorid:3^id~2^Author:single
Left Side:
	> 1:Genres,4:UserDetails:single
	
Explaining Right side:

### We are again splitting this part by (|)
Part A: 1^id:2^genreid~1^Books
Part B: 2^authorid:3^id~2^Author:single

> Important thing is that, the relations configuration should always will be considered from right to left. The last relations will be executed first and so on.
The above statement means "Part B" will be executed first and next "Part A" will be executed.
Let us see how the JSON object will be executed in the background . The sequence


### 1. Part B (2^authorid:3^id~2^Author:single): 
The data matching the below condition should be created in table "2" with Object Name as "Authors".
The extension of the Authors is single. This means we will be only be getting one row matching the below condition.
i.e For one book there will only be one author.
Condition : **table2.authorid = table3.id**

Let us see how the JSON will be created.
````
{	
	"Books":[{
			"genreid":"1",
			"BookID":3,
			"Title":"Scifi book 1",
			"authorid":1,
			"Author" :{
				"id":"1",
				"Name":"Author 1"
			}
		},{
			"genreid":"1",
			"BookID":4,
			"Title":"Scifi book 2",
			"authorid":2,
			"Author" :{
				"id":"2",
				"Name":"Author 2"
			}
		},{
			"genreid":"2",
			"BookID":5,
			"Title":"Thriller book 1",
			"authorid":3,
			"Author" :{
				"id":"3",
				"Name":"Author 3"
			}
		},{
			"genreid":"2",
			"BookID":6,
			"Title":"Thriller book 2",
			"authorid":1,
			"Author" :{
				"id":"1",
				"Name":"Author 1"
			}]
}
````
	
###	2. Part A (1^id:2^genreid~1^Books):
As the "Part B" JSON is created, let's see what the configuration in "Part A" does to this JSON.
The object should be created in table1 with Object Name as "Books".
Condtion : **table.id=table2.genreid**

The is how the JSON will be . This table1 JSON created with the configurations Part B & Part A will not have any root node.

````
[{
				"id" : "1",
				"GenreName":"Scifi",
				"Books":[{
					"genreid":"1",
					"BookID":3,
					"Title":"Scifi book 1",
					"authorid":1,
					"Author" :{
						"id":"1",
						"Name":"Author 1"
					}
				},{
					"genreid":"1",
					"BookID":4,
					"Title":"Scifi book 2",
					"authorid":2,
					"Author" :{
						"id":"2",
						"Name":"Author 2"
					}
				}]
			},
			{
				"id" : "2",
				"GenreName":"Thriller",
				"Books":[{
					"genreid":"2",
					"BookID":5,
					"Title":"Thriller book 1",
					"authorid":3,
					"Author" :{
						"id":"3",
						"Name":"Author 3"
					}
				},{
					"genreid":"2",
					"BookID":6,
					"Title":"Thriller book 2",
					"authorid":1,
					"Author" :{
						"id":"1",
						"Name":"Author 1"
					}
				}]
			}]
````


The "Right Side" of the configuration will only generate relations.
If you want the generated output in the JSON, then you should mention it in the "Left Side" in the below format.
1:Genres,4:UserDetails:single

Explaining:
*(1:Genres) All the data in table1 should be created in "Genres"
*(4:UserDetails:single) All the table4 data should be created in "UserDetails" as JSON Single object.

After the Formatter executed the "Rigth Side" & "Left Side" the output of JSON will be as below.

````
{
    "Genres":[{                  ----------------------------> table1 data
				"id" : "1",
				"GenreName":"Scifi",
				"Books":[{
					"genreid":"1",
					"BookID":3,
					"Title":"Scifi book 1",
					"authorid":1,
					"Author" :{
						"id":"1",
						"Name":"Author 1"
					}
				},{
					"genreid":"1",
					"BookID":4,
					"Title":"Scifi book 2",
					"authorid":2,
					"Author" :{
						"id":"2",
						"Name":"Author 2"
					}
				}]
			},
			{
				"id" : "2",
				"GenreName":"Thriller",
				"Books":[{
					"genreid":"2",
					"BookID":5,
					"Title":"Thriller book 1",
					"authorid":3,
					"Author" :{
						"id":"3",
						"Name":"Author 3"
					}
				},{
					"genreid":"2",
					"BookID":6,
					"Title":"Thriller book 2",
					"authorid":1,
					"Author" :{
						"id":"1",
						"Name":"Author 1"
					}
				}]
			}],
	"UserDetails":{					------------------> table4 data
		"FirstName":"my first name",
		"LastName":"my last name",
		"id":94
	}
}
````


Points to Note:

*Multiple relations are seperated by "|" in the config. (PartA|PartB)
*Multiple Output objects are seperated by "," in the config. (1:Genres,4:UserDetails:single)
*The left of seperator "!" should be Output objects and the right side should be relations.
*Use :single to get single json object. (ex: { "a":"a1","b":"b1"})
*Use :singlevar to get single variable object  (ex: "a":"a1" )
