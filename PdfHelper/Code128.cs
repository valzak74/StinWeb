using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfHelper
{
    /*
     *	Auteur	:	Joffrey VERDIER
     *	Date	:	08/2006
     *	Légal	:	OpenSource © 2007 AVRANCHES
     * 
     */

    class Code128
    {
        private static readonly Code128 _instance = new Code128();
        private Code128() { }
        public static Code128 Instance => _instance;
        /// <summary>
        /// Encode la chaine en code128
        /// </summary>
        /// <param name="chaine">Chaine à transcrire</param>
        /// <returns></returns>
        public String Encode(String chaine)
        {
            int ind = 1;
            int checksum = 0;
            int mini;
            int dummy;
            bool tableB;
            String code128;
            int longueur;

            code128 = "";
            longueur = chaine.Length;


            if (longueur == 0)
            {
                //Console.WriteLine("\n chaine vide");
            }
            else
            {
                for (ind = 0; ind < longueur; ind++)
                {
                    if ((chaine[ind] < 32) || (chaine[ind] > 126))
                    {
                        //Console.WriteLine("\n chaine invalide");
                    }
                }
            }


            tableB = true;
            ind = 0;



            while (ind < longueur)
            {

                if (tableB == true)
                {
                    if ((ind == 0) || (ind + 3 == longueur - 1))
                    {
                        mini = 4;
                    }
                    else
                    {
                        mini = 6;
                    }

                    mini = mini - 1;

                    if ((ind + mini) <= longueur - 1)
                    {
                        while (mini >= 0)
                        {
                            if ((chaine[ind + mini] < 48) || (chaine[ind + mini] > 57))
                            {
                                //Console.WriteLine("\n exit");
                                break;
                            }
                            mini = mini - 1;
                        }
                    }


                    if (mini < 0)
                    {
                        if (ind == 0)
                        {
                            code128 = Char.ToString((char)205);

                        }
                        else
                        {
                            code128 = code128 + Char.ToString((char)199);

                        }
                        tableB = false;
                    }
                    else
                    {

                        if (ind == 0)
                        {
                            code128 = Char.ToString((char)204);
                        }
                    }
                }

                if (tableB == false)
                {
                    mini = 2;
                    mini = mini - 1;
                    if (ind + mini < longueur)
                    {
                        while (mini >= 0)
                        {

                            if (((chaine[ind + mini]) < 48) || ((chaine[ind]) > 57))
                            {
                                break;
                            }
                            mini = mini - 1;
                        }
                    }
                    if (mini < 0)
                    {
                        dummy = Int32.Parse(chaine.Substring(ind, 2));

                        //Console.WriteLine("\n  dummy ici : " + dummy);

                        if (dummy < 95)
                        {
                            dummy = dummy + 32;
                        }
                        else
                        {
                            dummy = dummy + 100;
                        }
                        code128 = code128 + (char)(dummy);

                        ind = ind + 2;
                    }
                    else
                    {
                        code128 = code128 + Char.ToString((char)200);
                        tableB = true;
                    }
                }
                if (tableB == true)
                {

                    code128 = code128 + chaine[ind];
                    ind = ind + 1;
                }
            }

            for (ind = 0; ind <= code128.Length - 1; ind++)
            {
                dummy = code128[ind];
                //Console.WriteLine("\n  et voila dummy : " + dummy);
                if (dummy < 127)
                {
                    dummy = dummy - 32;
                }
                else
                {
                    dummy = dummy - 100;
                }
                if (ind == 0)
                {
                    checksum = dummy;
                }
                checksum = (checksum + (ind) * dummy) % 103;
            }

            if (checksum < 95)
            {
                checksum = checksum + 32;
            }
            else
            {
                checksum = checksum + 100;
            }
            code128 = code128 + Char.ToString((char)checksum)
                    + Char.ToString((char)206);

            return code128;
        }
    }
}
