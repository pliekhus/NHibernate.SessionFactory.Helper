using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NHibernate.SessionFactory.Helpers
{
	public class SqlError
	{
		public virtual string ErrorLine { get; set; }
		public virtual string ErrorMessage { get; set; }
		public virtual string ErrorNumber { get; set; }
		public virtual string ErrorProcedure { get; set; }
		public virtual string ErrorSeverity { get; set; }
		public virtual string ErrorState { get; set; }
	}
}